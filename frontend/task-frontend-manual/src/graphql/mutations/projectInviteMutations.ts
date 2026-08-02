    import {gql} from '@apollo/client';

    export const INVITE_EXISTING_USER_TO_PROJECT = gql`
      mutation InviteExistingUserToProject(
        $projectId: UUID!
        $email: String!
        $projectRole: String!
        $roleLabelId: UUID
      ) {
        inviteExistingUserToProject(
          projectId: $projectId
          email: $email
          projectRole: $projectRole
          roleLabelId: $roleLabelId
        )
      }
    `;