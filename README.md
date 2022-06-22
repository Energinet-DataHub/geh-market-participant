# Market Participant
[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-market-participant/branch/main/graph/badge.svg?token=1VGVTZG6IT)](https://codecov.io/gh/Energinet-DataHub/geh-market-participant)

Welcome to the Market Participant domain of the [Green Energy Hub project](https://github.com/Energinet-DataHub/green-energy-hub).

- [Intro](#intro)
- [Communicating with Market Participant](#Communicating-with-Market-Participant)
	 - [API](#API)
	 - [Integration Events](#Integration-events)

## Intro

Market Participant is where everything related to Organization, Actors, GridAreas and their relationships are handled.

## Communicating with Market Participant

Interaction with the Market Participant domain is done in two different ways, depending on whether you are looking to get data updates or want use the API to interact with the domain. 

A [Client Nuget Package](https://www.nuget.org/packages/Energinet.DataHub.MarketParticipant.Client/), which is the recommended way to interact with the domain is available, and exposes all the API's currently available

## API
The Following endpoints are available, seperated by concerns.

## Organization
#### GET:/Organization

```organization/```
*Returns all organizations*

#### GET:/Organization

```organization/{organizationId:guid}```
*Returns an organization with the specified id, if it exists.*

#### POST:/Organization

```organization/```
*Returns and organization with the specified id, if it exists.*

##### Example BODY:

```json
{
  "name": "string",
  "businessRegisterIdentifier": "string",
  "address": {
    "streetName": "string",
    "number": "string",
    "zipCode": "string",
    "city": "string",
    "country": "string"
  },
  "comment": "string"
}
```

#### PUT:/Organization

```organization/{organizationId:guid}```
*Updates an organization with the specified id, if it exists.*

##### Example BODY:

```json
{
  "name": "string",
  "businessRegisterIdentifier": "string",
  "address": {
    "streetName": "string",
    "number": "string",
    "zipCode": "string",
    "city": "string",
    "country": "string"
  },
  "comment": "string"
}
```
## Actor
#### GET:/Organization/Actor

```organization/{organizationId:guid}/actor/```
*Returns all actors in the specified organization and with the specified id, if it exists.*

#### GET:/Organization/Actor/ID

```organization/{organizationId:guid}/actor/{actorId:guid}```
*Returns the actor in the specified organization and with the specified id, if it exists.*

#### POST:/Organization/Actor/

```organization/{organizationId:guid}/actor/```
*Creates an Actor in the specified organization*

##### Example BODY:

```json
{
  "actorNumber": {
    "value": "string"
  },
  "gridAreas": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "marketRoles": [
    {
      "eicFunction": "string"
    }
  ],
  "meteringPointTypes": [
    "string"
  ]
}
```
#### PUT:/Organization/Actor/

```organization/{organizationId:guid}/actor/{actorId:guid}```
*Updates an Actor in the specified organization with the specified id, if it exists*

##### Example Body:
```json
{
  "status": "string",
  "gridAreas": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "marketRoles": [
    {
      "eicFunction": "string"
    }
  ],
  "meteringPointTypes": [
    "string"
  ]
}
```
## Contact
#### GET:/Organization/Contact

```organization/{organizationId:guid}/contact/```
*returns all contacts in the specified organization, if the organization exists.*

#### POST:/Organization/Contact

```organization/{organizationId:guid}/contact/```
*Creates a new contacts in the specified organization, if the organization exists.*

##### Example Body:
```json
{
  "name": "string",
  "category": "string",
  "email": "string",
  "phone": "string"
}
```

#### DELETE:/Organization/Contact

```organization/{organizationId:guid}/contact/{contactId:guid}```
*Deletes a contact from the specified organization, if it exists*

## Actor Contact

#### GET:/Organization/Actor/Contact

```organization/{organizationId:guid}/actor/{actorId:guid}/contact```
*returns all contacts for the specified actor in the specified organization, if the organization and actor exists.*

#### POST:/Organization/Actor/Contact

```organization/{organizationId:guid}/actor/{actorId:guid}/contact```
*Creates a contact for the specified actor in the specified organization, if the organization and actor exists.*

##### Example Body:
```json
{
  "name": "string",
  "category": "string",
  "email": "string",
  "phone": "string"
}
```

#### DELETE:/Organization/Actor/Contact

```organization/{organizationId:guid}/contact/{contactId:guid}```
*Deletes a contact from the specified organization, if it exists*

---
---

## Integration Events
Integration events are a way for interested parties to know when data changes in the Market Participant domain and be informed of these changes.

Integration events are published to a servicebus topic, where everyone who are interested can listen. What you do with these events are. A description of how to subscribe to these events are [here](#Subscribe-to-integration-events)

The following integrations events are available:

 - **Actor Events**
	 - [ActorCreated](#integration-event-actor-created)
	 - [ActorStatusChanged](#integration-event-actor-status-changed)
	 - [ActorRoleAdded](#integration-event-actor-role-added)
	 - [ActorRoleRemoved](#integration-event-actor-role-removed)
	 - [ActorContactAdded](#integration-event-actor-contact-added)
	 - [ActorContactRemoved](#integration-event-actor-contact-removed)
	 - [ActorGridAreaAdded](#integration-event-actor-gridarea-added)
	 - [ActorGridAreaRemoved](#integration-event-actor-gridarea-removed)
 - **Organization Events**
	 - [OrganizationCreated](#integration-event-organization-created)
	 - [OrganizationNameChanged](#integration-event-organization-name-changed)
	 - [OrganizationAddressChanged](#integration-event-organization-address-changed)
	 - [OrganizationCommentChanged](#integration-event-organization-comment-changed)
	 - [OrganizationBusinessRegisterIdentifierChanged](#integration-event-actor-created)
 - **Grid Areas**
	 - [GridAreaCreated](#integration-event-actor-created)
	 - [GridAreaNameChanged](#integration-event-actor-created)
	

