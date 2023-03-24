workspace extends https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/main/source/datahub3-model/model.dsl {
    model {
        !ref dh3 {
            dh3WebApp = group "Web App." {
                frontend = container "UI" "Provides DH3 functionality to users via their web browser." "Angular"
                bff = container "Backend for frontend" "Combines data for presentation on DataHub 3 UI." "Asp.Net Core Web API"

                frontend -> bff "Uses" "JSON/HTTPS"
            }

            markpart = group "Market Participant" {
                markpartApi = container "Market Participant API" "Multi-tenant API for managing actors, users and permissions." "Asp.Net Core Web API"
                markpartEventActorSynchronizer = container "Actor Synchronizer" "Creates B2C application registration for newly created actors." "Azure Function App (Timer Trigger)" "Microsoft Azure - Function Apps"
                markpartMailDispatcher = container "Mail Dispatcher" "Responsible for sending user invitations." "Azure Function App (Timer Trigger)" "Microsoft Azure - Function Apps"
                markpartDb = container "Database" "Stores data regarding actors, users and permissions." "SQL Database Schema" "Data Storage,Microsoft Azure - SQL Database"

                markpartApi -> markpartDb "Reads and writes actor/user data." "Entity Framework Core"
                markpartEventActorSynchronizer -> markpartDb "Updates actors with external B2C id" "Entity Framework Core"
                markpartMailDispatcher -> markpartDb "Reads data regarding newly invited users" "Entity Framework Core"
            }

            shared = group "Shared resources" {
                sharedBus = container "Azure B2C" "" "" "Microsoft Azure"
            }
        }

        sendGrid = softwareSystem "SendGrid" "External 3rd party mail dispatcher"

        dh3User -> frontend "Views and manages data across all actors"
        extUser -> frontend "Views and manages data within the users assigned actor"
        bff -> markpartApi "Uses" "JSON/HTTPS"
        markpartEventActorSynchronizer -> sharedBus "Creates B2C application registration" "Microsoft.Graph/HTTPS"
        markpartMailDispatcher -> sendGrid "Sends invitation mail" "SendGrid/HTTPS"
        sendGrid -> extUser "Sends invitation mail"
    }

    views {
        container dh3 {
            include *
            autolayout
        }

        container dh3 "WebApp" {
            title "DataHub 3.0 - Market Participant - WebApp"
            description "Level 2"
            include dh3WebApp markpartApi
            autolayout
        }

        container dh3 "MarketParticipant" {
            title "DataHub 3.0 - Market Participant"
            description "Level 2"
            include markpart
            autolayout
        }
    }
}
