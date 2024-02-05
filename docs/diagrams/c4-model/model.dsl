# Read description in the 'views.dsl' file.

markpartDomain = group "Market Participant" {
    #
    # Common (managed by Market Participant)
    #
    
    #
    # Domain
    #
    markpartDb = container "Market Participant Database" {
        description "Stores data regarding actors, users and permissions."
        technology "SQL Database Schema"
        tags "Data Storage" "Microsoft Azure - SQL Database" "Titans"
    }

    markpartKeyVault = container "Market Participant Internal Key Vault" {
        description "Stores key used for signing tokens."
        technology "Azure Key Vault"
        tags "Microsoft Azure - Key Vaults" "Titans"
    }

    markpartCertKeyVault = container "Market Participant Certificate Key Vault" {
        description "Stores public DH2 certificates used for B2B authentication."
        technology "Azure Key Vault"
        tags "Microsoft Azure - Key Vaults" "Titans"
    }

    markpartApi = container "Market Participant API" {
        description "Multi-tenant API for managing actors, users and permissions."
        technology "Asp.Net Core Web API"
        tags "Microsoft Azure - App Services" "Titans"

        # Common relationships
        this -> dh3.sharedB2C "Creates and manages B2C users" "Microsoft.Graph/https"

        # Domain relationships
        this -> markpartDb "Reads and writes actor/user data." "EF Core"
        this -> markpartKeyVault "Signs, and reads public key for, tokens." "Microsoft.Graph/https"
        this -> markpartCertKeyVault "Manages active DH2 certificates used for B2B authentication." "Microsoft.Graph/https"

        markpartUserIdentityRepositoryInMarkpartApi = component "UserIdentityRepository" {
            description "Manages access to B2C user data through GraphAPI."

            # Common relationships
            this -> dh3.sharedB2C "Accesses user information" "Microsoft.Graph/https"
        }
        
        markpartUserController = component "UserController" {
            description "API for managing users."

            # Domain relationships
            this -> markpartDb "Writes/reads from" "EF Core"

            # Common relationships
            this -> markpartUserIdentityRepositoryInMarkpartApi "Accesses user information" "Microsoft.Graph/https"
        }

        markpartPermissionController = component "PermissionController" {
            description "API for managing system permissions."

            # Domain relationships
            this -> markpartDb "Writes/reads from" "EF Core"
        }

        markpartUserRoleController = component "UserRoleController" {
            description "API for managing user roles and underlying system permissions."

            # Domain relationships
            this -> markpartDb "Writes/reads from" "EF Core"
        }

        markpartUserRoleAssignmentController = component "UserRoleAssignmentController" {
            description "API for managing users and their assigned user roles."

            # Domain relationships
            this -> markpartDb "Writes/reads from" "EF Core"

            # Common relationships
            this -> markpartUserIdentityRepositoryInMarkpartApi "Accesses user information" "Microsoft.Graph/https"
        }

        markpartUserOverviewController = component "UserOverviewController" {
            description "Read-only API for filtered listing of users for frontend presentation"

            # Domain relationships
            this -> markpartDb "Reads from" "EF Core"

            # Common relationships
            this -> markpartUserIdentityRepositoryInMarkpartApi "Accesses user information" "Microsoft.Graph/https"
        }

        markpartInvitationController = component "InvitationController" {
            description "API for new user invitations as well as additional actor invitations for existing users. Invitations are only prepared for dispatch here."

            # Domain relationships
            this -> markpartDb "Writes/reads from" "EF Core"

            # Common relationships
            this -> markpartUserIdentityRepositoryInMarkpartApi "Accesses user information" "Microsoft.Graph/https"
            bffApi -> this
        }
    }

    markpartCertificateSynchronization = container "Market Participant <Certificate Synchronization>" {
        description "Synchronizes active DH2 authentication certificates with APIM."
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Titans"

        # Common relationships
        this -> dh3.sharedApiManagement "Links and unlinks DH2 active authentication certificates." "REST/https"

        # Domain relationships
        this -> markpartCertKeyVault "Gets certificates that should be active in APIM." "Microsoft.Graph/https"
    }

    markpartOrganizationManager = container "Market Participant <Organization Manager>" {
        description "Synchronizes Azure B2C user and actor state with the domain."
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Titans"

        markpartUserIdentityRepositoryInOrganizationManager = component "UserIdentityRepository" {
            description "Manages access to B2C user data through GraphAPI."

            # Common relationships
            this -> dh3.sharedB2C "Accesses user information" "Microsoft.Graph/https"
        }

        markpartEventActorSynchronizer = component "Actor Synchronizer" {
            description "Creates B2C application registration for newly created actors."
            technology "Timer Trigger"
            tags "Microsoft Azure - Function Apps" "Titans"

            # Common relationships
            this -> dh3.sharedB2C "Creates B2C App Registration" "Microsoft.Graph/https"
            this -> dh3.sharedServiceBus "Sends market participant events" "integration event/amqp"

            # Domain relationships
            this -> markpartDb "Updates actors with external B2C id." "EF Core"
        }

        markpartMailDispatcher = component "Mail Dispatcher" {
            description "Responsible for sending user invitations."
            technology "Timer Trigger"
            tags "Microsoft Azure - Function Apps" "Titans"

            # Common relationships
            this -> dh3.sharedExternalSendGrid "Sends invitation mail" "SendGrid/https"

            # Domain relationships
            this -> markpartDb "Reads data regarding newly invited users." "EF Core"
        }

        markpartUserInvitationExpiredTimerTrigger = component "UserInvitationExpiredTimerTrigger" {
            description "Timer Trigger checking for users with expired invitations. Users with expired invitations are disable in B2C."

            # Domain relationships
            this -> markpartDb "Reads from" "EF Core"

            # Common relationships
            this -> markpartUserIdentityRepositoryInOrganizationManager "Accesses user information" "Microsoft.Graph/https"
        }
    }
}
