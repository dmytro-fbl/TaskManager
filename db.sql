/* 
create extension if not exists pgcrypto;
drop schema if exists app cascade;
create schema app;

-------------------------------------------------------------------------------
-- 1. USERS
-------------------------------------------------------------------------------
create table if not exists app.users (
    id uuid primary key default gen_random_uuid(),
    name varchar(120) not null,
    email varchar(255) not null unique,
    password_hash text not null,
    password_salt text not null,
    avatar_url text,
    is_admin boolean not null default false,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint chk_users_name_len check (char_length(trim(name)) >= 2),
    constraint chk_users_email_len check (char_length(trim(email)) >= 5)
);

-------------------------------------------------------------------------------
-- 2. PROJECT TEMPLATES
-------------------------------------------------------------------------------
create table if not exists app.project_templates (
    id uuid primary key default gen_random_uuid(),
    name varchar(180) not null,
    description text,
    created_by uuid not null,
    is_archived boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint fk_project_templates_created_by
        foreign key (created_by) references app.users(id) on delete restrict,
    constraint chk_project_templates_name_len
        check (char_length(trim(name)) >= 2)
);

create table if not exists app.project_template_owners (
    template_id uuid not null,
    user_id uuid not null,
    added_at timestamptz not null default now(),
    primary key (template_id, user_id),
    constraint fk_project_template_owners_template
        foreign key (template_id) references app.project_templates(id) on delete cascade,
    constraint fk_project_template_owners_user
        foreign key (user_id) references app.users(id) on delete cascade
);

create table if not exists app.template_statuses (
    id uuid primary key default gen_random_uuid(),
    template_id uuid not null,
    name varchar(80) not null,
    category varchar(20) not null default 'todo',
    color varchar(30),
    sort_order integer not null default 0,
    is_final boolean not null default false,
    created_at timestamptz not null default now(),
    constraint fk_template_statuses_template
        foreign key (template_id) references app.project_templates(id) on delete cascade,
    constraint uq_template_statuses_template_name unique (template_id, name),
    constraint uq_template_statuses_template_order unique (template_id, sort_order),
    constraint chk_template_statuses_name
        check (char_length(trim(name)) >= 2),
    constraint chk_template_statuses_order
        check (sort_order >= 0),
    constraint chk_template_statuses_category
        check (category in ('todo', 'in_progress', 'done'))
);

create table if not exists app.template_role_labels (
    id uuid primary key default gen_random_uuid(),
    template_id uuid not null,
    name varchar(100) not null,
    created_at timestamptz not null default now(),
    constraint fk_template_role_labels_template
        foreign key (template_id) references app.project_templates(id) on delete cascade,
    constraint uq_template_role_labels_template_name unique (template_id, name),
    constraint chk_template_role_labels_name
        check (char_length(trim(name)) >= 2)
);

-------------------------------------------------------------------------------
-- 3. PROJECTS
-------------------------------------------------------------------------------
create table if not exists app.projects (
    id uuid primary key default gen_random_uuid(),
    title varchar(180) not null,
    description text,
    budget_cap numeric(12,2),
    status varchar(20) not null default 'active',
    owner_id uuid not null,
    is_archived boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint fk_projects_owner
        foreign key (owner_id) references app.users(id) on delete restrict,
    constraint chk_projects_title_len
        check (char_length(trim(title)) >= 2),
    constraint chk_projects_budget_cap
        check (budget_cap is null or budget_cap >= 0),
    constraint chk_projects_status
        check (status in ('active', 'on_hold', 'completed', 'archived'))
);

create table if not exists app.project_role_labels (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    name varchar(100) not null,
    -- Фінанси (unit_rate, rate_unit) видалено
    created_at timestamptz not null default now(),
    constraint fk_project_role_labels_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint uq_project_role_labels_project_name unique (project_id, name),
    constraint chk_project_role_labels_name
        check (char_length(trim(name)) >= 2)
);

create table if not exists app.project_statuses (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    name varchar(80) not null,
    category varchar(20) not null default 'todo',
    color varchar(30),
    sort_order integer not null default 0,
    is_final boolean not null default false,
    created_at timestamptz not null default now(),
    constraint fk_project_statuses_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint uq_project_statuses_project_name unique (project_id, name),
    constraint uq_project_statuses_project_order unique (project_id, sort_order),
    constraint chk_project_statuses_name
        check (char_length(trim(name)) >= 2),
    constraint chk_project_statuses_order
        check (sort_order >= 0),
    constraint chk_project_statuses_category
        check (category in ('todo', 'in_progress', 'done'))
);

create table if not exists app.project_memberships (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    user_id uuid not null,
    project_role varchar(20) not null,
    role_label_id uuid,
    joined_at timestamptz not null default now(),
    constraint fk_project_memberships_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_project_memberships_user
        foreign key (user_id) references app.users(id) on delete cascade,
    constraint fk_project_memberships_role_label
        foreign key (role_label_id) references app.project_role_labels(id) on delete set null,
    constraint uq_project_memberships_project_user unique (project_id, user_id),
    constraint chk_project_memberships_role
        check (project_role in ('manager', 'contributor'))
);

-------------------------------------------------------------------------------
-- 4. TASKS & WORKLOGS
-------------------------------------------------------------------------------
create table if not exists app.tasks (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    author_id uuid not null,
    assignee_id uuid,
    status_id uuid not null,
    title varchar(200) not null,
    notes text,
    priority varchar(20) not null default 'medium',
    start_date date,
    due_date date,
    estimated_budget numeric(12,2),
    estimated_unit varchar(10),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    completed_at timestamptz,
    constraint fk_tasks_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_tasks_author
        foreign key (author_id) references app.users(id) on delete restrict,
    constraint fk_tasks_assignee
        foreign key (assignee_id) references app.users(id) on delete set null,
    constraint fk_tasks_status
        foreign key (status_id) references app.project_statuses(id) on delete restrict,
    constraint chk_tasks_title_len
        check (char_length(trim(title)) >= 2),
    constraint chk_tasks_priority
        check (priority in ('low', 'medium', 'high', 'critical')),
    constraint chk_tasks_estimated_budget
        check (estimated_budget is null or estimated_budget >= 0),
    constraint chk_tasks_estimated_unit
        check (estimated_unit is null or estimated_unit in ('hours', 'days', 'fixed')),
    constraint chk_tasks_dates
        check (due_date is null or start_date is null or due_date >= start_date)
);

create table if not exists app.task_comments (
    id uuid primary key default gen_random_uuid(),
    task_id uuid not null,
    author_id uuid not null,
    body text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    is_edited boolean not null default false,
    constraint fk_task_comments_task
        foreign key (task_id) references app.tasks(id) on delete cascade,
    constraint fk_task_comments_author
        foreign key (author_id) references app.users(id) on delete restrict,
    constraint chk_task_comments_body
        check (char_length(trim(body)) >= 1)
);

create table if not exists app.task_comment_versions (
    id uuid primary key default gen_random_uuid(),
    comment_id uuid not null,
    previous_body text not null,
    changed_at timestamptz not null default now(),
    constraint fk_task_comment_versions_comment
        foreign key (comment_id) references app.task_comments(id) on delete cascade
);

create table if not exists app.task_attachments (
    id uuid primary key default gen_random_uuid(),
    task_id uuid not null,
    uploaded_by uuid not null,
    file_name varchar(255) not null,
    file_url text not null,
    mime_type varchar(120),
    size_bytes bigint,
    uploaded_at timestamptz not null default now(),
    constraint fk_task_attachments_task
        foreign key (task_id) references app.tasks(id) on delete cascade,
    constraint fk_task_attachments_uploaded_by
        foreign key (uploaded_by) references app.users(id) on delete restrict,
    constraint chk_task_attachments_file_name
        check (char_length(trim(file_name)) >= 1),
    constraint chk_task_attachments_size
        check (size_bytes is null or size_bytes >= 0),
    constraint chk_task_attachments_max_size
        check (size_bytes is null or size_bytes <= 10485760)
);

create table if not exists app.worklogs (
    id uuid primary key default gen_random_uuid(),
    task_id uuid not null,
    user_id uuid not null,
    role_label_id uuid,
    hours_spent numeric(6,2) not null,
    log_date date not null default current_date,
    description text,
    created_at timestamptz not null default now(),
    constraint fk_worklogs_task
        foreign key (task_id) references app.tasks(id) on delete cascade,
    constraint fk_worklogs_user
        foreign key (user_id) references app.users(id) on delete restrict,
    constraint fk_worklogs_role_label
        foreign key (role_label_id) references app.project_role_labels(id) on delete set null,
    constraint chk_worklogs_hours
        check (hours_spent > 0)
);

-------------------------------------------------------------------------------
-- 5. NOTES
-------------------------------------------------------------------------------
create table if not exists app.notes (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    parent_note_id uuid,
    author_id uuid not null,
    title varchar(120) not null,
    body_md text not null default '',
    is_restricted boolean not null default false,
    sort_order integer not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint fk_notes_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_notes_parent
        foreign key (parent_note_id) references app.notes(id) on delete cascade,
    constraint fk_notes_author
        foreign key (author_id) references app.users(id) on delete restrict,
    constraint chk_notes_title_len
        check (char_length(trim(title)) >= 1),
    constraint chk_notes_sort_order
        check (sort_order >= 0)
);

create table if not exists app.note_collaborators (
    note_id uuid not null,
    user_id uuid not null,
    permission varchar(10) not null,
    granted_at timestamptz not null default now(),
    primary key (note_id, user_id),
    constraint fk_note_collaborators_note
        foreign key (note_id) references app.notes(id) on delete cascade,
    constraint fk_note_collaborators_user
        foreign key (user_id) references app.users(id) on delete cascade,
    constraint chk_note_collaborators_permission
        check (permission in ('view', 'edit'))
);

-------------------------------------------------------------------------------
-- 6. ARTIFACTS
-------------------------------------------------------------------------------
create table if not exists app.artifacts (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    parent_artifact_id uuid,
    uploaded_by uuid not null,
    title varchar(180) not null,
    description text,
    type varchar(20) not null,
    url text,
    storage_key text,
    mime_type varchar(120),
    size_bytes bigint,
    is_archived boolean not null default false,
    sort_order integer not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint fk_artifacts_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_artifacts_parent
        foreign key (parent_artifact_id) references app.artifacts(id) on delete cascade,
    constraint fk_artifacts_uploaded_by
        foreign key (uploaded_by) references app.users(id) on delete restrict,
    constraint chk_artifacts_title_len
        check (char_length(trim(title)) >= 1),
    constraint chk_artifacts_type
        check (type in ('folder', 'file', 'link', 'embed')),
    constraint chk_artifacts_size
        check (size_bytes is null or size_bytes >= 0),
    constraint chk_artifacts_sort_order
        check (sort_order >= 0),
    constraint chk_artifacts_folder_payload
        check (
            (type = 'folder' and url is null and storage_key is null and mime_type is null and size_bytes is null)
            or (type <> 'folder')
        )
);

create table if not exists app.artifact_versions (
    id uuid primary key default gen_random_uuid(),
    artifact_id uuid not null,
    version_no integer not null,
    file_name varchar(255) not null,
    storage_key text not null,
    mime_type varchar(120),
    size_bytes bigint,
    uploaded_by uuid not null,
    created_at timestamptz not null default now(),
    constraint fk_artifact_versions_artifact
        foreign key (artifact_id) references app.artifacts(id) on delete cascade,
    constraint fk_artifact_versions_uploaded_by
        foreign key (uploaded_by) references app.users(id) on delete restrict,
    constraint uq_artifact_versions_artifact_version unique (artifact_id, version_no),
    constraint chk_artifact_versions_version_no
        check (version_no > 0),
    constraint chk_artifact_versions_size
        check (size_bytes is null or size_bytes >= 0)
);

-------------------------------------------------------------------------------
-- 7. INVITATIONS, SUBSCRIPTIONS, NOTIFICATIONS
-------------------------------------------------------------------------------
create table if not exists app.invitations (
    id uuid primary key default gen_random_uuid(),
    project_id uuid not null,
    email varchar(255) not null,
    token varchar(255) not null unique,
    project_role varchar(20) not null,
    role_label_id uuid,
    invited_by uuid not null,
    expires_at timestamptz not null,
    accepted_at timestamptz,
    created_at timestamptz not null default now(),
    constraint fk_invitations_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_invitations_role_label
        foreign key (role_label_id) references app.project_role_labels(id) on delete set null,
    constraint fk_invitations_invited_by
        foreign key (invited_by) references app.users(id) on delete restrict,
    constraint chk_invitations_role
        check (project_role in ('manager', 'contributor')),
    constraint chk_invitations_email_len
        check (char_length(trim(email)) >= 5),
    constraint chk_invitations_expiry
        check (expires_at > created_at)
);

create table if not exists app.subscriptions (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null,
    task_id uuid,
    project_id uuid,
    event_types jsonb not null default '[]'::jsonb,
    created_at timestamptz not null default now(),
    constraint fk_subscriptions_user
        foreign key (user_id) references app.users(id) on delete cascade,
    constraint fk_subscriptions_task
        foreign key (task_id) references app.tasks(id) on delete cascade,
    constraint fk_subscriptions_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint chk_subscriptions_target
        check (
            (task_id is not null and project_id is null)
            or
            (task_id is null and project_id is not null)
        )
);

create table if not exists app.notifications (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null,
    actor_id uuid,
    type varchar(40) not null,
    message text not null,
    link text,
    is_read boolean not null default false,
    created_at timestamptz not null default now(),
    constraint fk_notifications_user
        foreign key (user_id) references app.users(id) on delete cascade,
    constraint fk_notifications_actor
        foreign key (actor_id) references app.users(id) on delete set null,
    constraint chk_notifications_message_len
        check (char_length(trim(message)) >= 1)
);

-------------------------------------------------------------------------------
-- 8. CHANGE HISTORY
-------------------------------------------------------------------------------
create table if not exists app.change_history (
    id uuid primary key default gen_random_uuid(),
    project_id uuid,
    entity_type varchar(40) not null,
    entity_id uuid not null,
    changed_by uuid not null,
    action varchar(20) not null,
    diff jsonb not null,
    changed_at timestamptz not null default now(),
    constraint fk_change_history_project
        foreign key (project_id) references app.projects(id) on delete cascade,
    constraint fk_change_history_changed_by
        foreign key (changed_by) references app.users(id) on delete restrict,
    constraint chk_change_history_entity_type_len
        check (char_length(trim(entity_type)) >= 2)
);

-------------------------------------------------------------------------------
-- 9. INDEXES
-------------------------------------------------------------------------------
create index if not exists idx_projects_owner_id
    on app.projects(owner_id);

create index if not exists idx_project_role_labels_project_id
    on app.project_role_labels(project_id);

create index if not exists idx_project_statuses_project_id
    on app.project_statuses(project_id);

create index if not exists idx_project_memberships_project_id
    on app.project_memberships(project_id);

create index if not exists idx_project_memberships_user_id
    on app.project_memberships(user_id);

create index if not exists idx_tasks_project_id
    on app.tasks(project_id);

create index if not exists idx_tasks_author_id
    on app.tasks(author_id);

create index if not exists idx_tasks_assignee_id
    on app.tasks(assignee_id);

create index if not exists idx_tasks_status_id
    on app.tasks(status_id);

create index if not exists idx_tasks_due_date
    on app.tasks(due_date);

create index if not exists idx_task_comments_task_id
    on app.task_comments(task_id);

create index if not exists idx_task_comment_versions_comment_id
    on app.task_comment_versions(comment_id);

create index if not exists idx_task_attachments_task_id
    on app.task_attachments(task_id);

create index if not exists idx_project_templates_created_by
    on app.project_templates(created_by);

create index if not exists idx_project_template_owners_user_id
    on app.project_template_owners(user_id);

create index if not exists idx_template_statuses_template_id
    on app.template_statuses(template_id);

create index if not exists idx_template_role_labels_template_id
    on app.template_role_labels(template_id);

create index if not exists idx_notes_project_id
    on app.notes(project_id);

create index if not exists idx_notes_parent_note_id
    on app.notes(parent_note_id);

create index if not exists idx_notes_author_id
    on app.notes(author_id);

create index if not exists idx_note_collaborators_user_id
    on app.note_collaborators(user_id);

create index if not exists idx_artifacts_project_id
    on app.artifacts(project_id);

create index if not exists idx_artifacts_parent_artifact_id
    on app.artifacts(parent_artifact_id);

create index if not exists idx_artifacts_uploaded_by
    on app.artifacts(uploaded_by);

create index if not exists idx_artifact_versions_artifact_id
    on app.artifact_versions(artifact_id);

create index if not exists idx_invitations_project_id
    on app.invitations(project_id);

create index if not exists idx_invitations_email
    on app.invitations(email);

create index if not exists idx_subscriptions_user_id
    on app.subscriptions(user_id);

create index if not exists idx_subscriptions_task_id
    on app.subscriptions(task_id);

create index if not exists idx_subscriptions_project_id
    on app.subscriptions(project_id);

create index if not exists idx_notifications_user_id
    on app.notifications(user_id);

create index if not exists idx_notifications_user_unread
    on app.notifications(user_id) where is_read = false;

create index if not exists idx_change_history_project
    on app.change_history(project_id);

create index if not exists idx_change_history_entity
    on app.change_history(entity_type, entity_id);

create index if not exists idx_change_history_changed_by
    on app.change_history(changed_by);

create index if not exists idx_worklogs_task_id
    on app.worklogs(task_id);

create index if not exists idx_worklogs_user_id
    on app.worklogs(user_id);

create index if not exists idx_worklogs_role_label_id
    on app.worklogs(role_label_id);

ALTER TABLE app.users ALTER COLUMN password_hash DROP NOT NULL;
ALTER TABLE app.users ALTER COLUMN password_salt DROP NOT NULL;

ALTER TABLE app.users ADD COLUMN invite_token varchar(255) UNIQUE;
ALTER TABLE app.users ADD COLUMN invite_expires_at timestamptz;