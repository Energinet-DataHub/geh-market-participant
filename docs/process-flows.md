# Actor register

Documentation regarding the actor register will be gathered here.

## Integration events

This section describes flows, how to use integration events and which data are transmitted.

The following image depicts the process flows. Notice that a timer trigger reads new events from the database every 5 minutes. These events are published to a message queue that other domains can subscribe to.

![Flow diagram for all integration events](./images/Actor_register-Integration%20Events%20Flow%20diagram.drawio.png)
<b>Fig. 1 - Flow diagram for all integration events</b>

To illustrate which classes are involved in the events that are currently raised, see the image below.

![Class diagram for the integration events in the actor register](./images/Actor_register-IntegrationsEvents.drawio.png)
<b>Fig. 2 - Simplified class diagram for integration events</b>

Data transmitted in the integration events are listed below. To avoid duplicate business logic that maps BusinessRoles and MarketRoles, both are sent to the domains in ActorUpdatedIntegrationEvent.

![Data transmitted in the current integration events](./images/Actor_register-IntegrationEventsDataTransmitted.drawio.png)
<b>Fig. 3 - Data transmitted in integration events</b>