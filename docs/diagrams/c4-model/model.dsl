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
    markpartApi = container "Market Participant API" {
        description "Multi-tenant API for managing actors, users and permissions."
        technology "Asp.Net Core Web API"
        tags "Microsoft Azure - App Services" "Titans"

        # Common relationships
        this -> dh3.sharedB2C "Creates and manages B2C users" "Microsoft.Graph/https"

        # Domain relationships
        this -> markpartDb "Reads and writes actor/user data." "EF Core"
    }
    markpartOrganizationManager = container "Market Participant <Organization Manager>" {
        description "<add description>"
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
            this -> dh3.sharedSendGrid "Sends invitation mail" "SendGrid/https"

            # Domain relationships
            this -> markpartDb "Reads data regarding newly invited users." "EF Core"
        }
    }
}

