# The 'views.dsl' file is intended as a mean for viewing and validating the model
# in the domain repository. It should
#   * Extend the base model and override the 'dh3' software system
#   * Include of the `model.dsl` files from each domain repository using a URL
#
# The `model.dsl` file must contain the actual model, and is the piece that must
# be reusable and included in the complete model file located in the 'dh3-infrastructure' repository.

# TODO: Reset to use main before merging PR
workspace extends https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/dstenroejl/align-c4-models/docs/diagrams/c4-model/dh3-base-model.dsl {

    model {
        #
        # DataHub 3.0 (extends)
        #
        !ref dh3 {

            # IMPORTANT:
            # The order by which models are included is important for how the domain-to-domain relationships are specified.
            # A domain-to-domain relationship should be specified in the "client" of a "client->server" dependency, and
            # hence domains that doesn't depend on others, should be listed first.

            # Include Market Participant model
            !include https://raw.githubusercontent.com/Energinet-DataHub/dh3-infrastructure/dstenroejl/continue-refactoring-of-diagrams/c4-model/markpart-model.dsl?token=GHSAT0AAAAAABXFU5ELD2AA7J6ZXRVDWO2SZCSWLPQ

            # Include EDI model
            !include https://raw.githubusercontent.com/Energinet-DataHub/dh3-infrastructure/dstenroejl/continue-refactoring-of-diagrams/c4-model/edi-model.dsl?token=GHSAT0AAAAAABXFU5ELB6EQCIJH32UKF5SKZCSWLWA

            # Include Wholesale model
            !include https://raw.githubusercontent.com/Energinet-DataHub/dh3-infrastructure/dstenroejl/continue-refactoring-of-diagrams/c4-model/wholesale-model.dsl?token=GHSAT0AAAAAABXFU5EKDQKDJ5FWFVSUT2AMZCSWL6A

            # Include Frontend model
            !include https://raw.githubusercontent.com/Energinet-DataHub/dh3-infrastructure/dstenroejl/continue-refactoring-of-diagrams/c4-model/frontend-model.dsl?token=GHSAT0AAAAAABXFU5ELQPQKK4OYQHQJJ3PEZCSWMEA

            # Include Migration model
            !include https://raw.githubusercontent.com/Energinet-DataHub/dh3-infrastructure/dstenroejl/continue-refactoring-of-diagrams/c4-model/migration-model.dsl?token=GHSAT0AAAAAABXFU5EL53FWD66NOSQXBBOKZCSWMKA
        }
    }

    views {
        container dh3 "MarketParticipant" {
            title "[Container] DataHub 3.0 - Market Participant (Simplified)"
            include ->markpartDomain->
            exclude "relationship.tag==OAuth"
            exclude "element.tag==Intermediate Technology"
            autolayout
        }

        container dh3 "MarketParticipantDetailed" {
            title "[Container] DataHub 3.0 - Market Participant (Detailed with OAuth)"
            include ->markpartDomain->
            exclude "relationship.tag==Simple View"
            autolayout
        }
    }
}