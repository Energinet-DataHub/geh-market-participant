# Read description in the 'views.dsl' file.

markpartDomain = group "Market Participant" {
    #
    # Common (managed by Market Participant)
    #
    
    #
    # Domain
    #
    markpartDb = container "Actors Database" {
        description "Stores data regarding actors, users and permissions."
        technology "SQL Database Schema"
        tags "Data Storage" "Microsoft Azure - SQL Database" "Titans"
    }
    markpartKeyVault = container "Market Participant Internal Key Vault" {
        description "Stores key used for signing tokens."
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
    }
    markpartOrganizationManager = container "Market Participant <Organization Manager>" {
        description "Synchronizes Azure B2C user and actor state with the domain."
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Titans"

        markpartEventActorSynchronizer = component "Actor Synchronizer" {
            description "Creates B2C application registration for newly created actors."
            technology "Timer Trigger"
            tags "Microsoft Azure - Function Apps" "Titans"

            # Common relationships
            this -> dh3.sharedB2C "Creates B2C App Registration" "Microsoft.Graph/https"

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
    }
}

