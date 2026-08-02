import React, { useRef } from 'react';
import { AddUserToProjectForm } from './InviteExistingUserForm';
import { ProjectMembersTable } from './ProjectMembersTable'; 
interface ProjectMembersTabProps {
    projectId: string;
}

export const ProjectMembersTab: React.FC<ProjectMembersTabProps> = ({ projectId }) => {
    const tableRef = useRef<{ refetchMembers: () => void }>(null);

    const handleUserAdded = () => {
        if (tableRef.current) {
            tableRef.current.refetchMembers();
        }
    };

    return (
        <div className="space-y-6">
            {/* Форма додавання */}
            <AddUserToProjectForm 
                projectId={projectId} 
                onUserAdded={handleUserAdded} 
            />

            {/* Таблиця учасників */}
            <ProjectMembersTable 
                projectId={projectId} 
                ref={tableRef} 
            />
        </div>
    );
};