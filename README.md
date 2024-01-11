# Market Participant

Welcome to the Market Participant domain of the [Green Energy Hub project](https://github.com/Energinet-DataHub/green-energy-hub).

- [Introduction](#introduction)
- [Communicating with Market Participant](#communicating-with-market-participant)
    - [API](#api)
        - [Organization](#organization)
        - [Actor](#actor)
        - [Actor Contact](#actor-contact)
        - [Grid Area](#grid-area)
- [Domain C4 model](#domain-c4-model)

## Introduction

Market Participant is where everything related to Organization, Actors, Grid Areas and their relationships are handled.

## Communicating with Market Participant

Interaction with the Market Participant domain is done in two different ways, depending on whether you are looking to get data updates or want use the API to interact with the domain.

A [Client Nuget Package](https://www.nuget.org/packages/Energinet.DataHub.MarketParticipant.Client/), which is the recommended way to interact with the domain is available, and exposes all the API's currently available

## Integration Events

Market Participant publishes several integration events.
A list of the integration event contracts and their documentation can be found at [/source/marketparticipant/Energinet.DataHub.MarketParticipant.Infrastructure/Model/Contracts](./source/marketparticipant/Energinet.DataHub.MarketParticipant.Infrastructure/Model/Contracts)

## API

The Following endpoints are available, separated by concerns.

## Organization

### GET:/Organization

```organization/```<br />
*Returns all organizations*

### GET:/Organization/ID

```organization/{organizationId:guid}```<br />
*Returns an organization with the specified id, if it exists.*

### POST:/Organization

```organization/```<br />
*Creates a new organization with the specified data*

**Example body:**

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

```organization/{organizationId:guid}```<br />
*Updates an organization with the specified id, if it exists.*

**Example body:**

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

### GET:/Organization/Actor

```organization/{organizationId:guid}/actor/```<br />
*Returns all actors in the specified organization and with the specified id, if it exists.*

### GET:/Organization/Actor/ID

```organization/{organizationId:guid}/actor/{actorId:guid}```<br />
*Returns the actor in the specified organization and with the specified id, if it exists.*

### POST:/Organization/Actor/

```organization/{organizationId:guid}/actor/```<br />
*Creates an Actor in the specified organization*

**Example body:**

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

### PUT:/Organization/Actor/

```organization/{organizationId:guid}/actor/{actorId:guid}```<br />
*Updates an Actor in the specified organization with the specified id, if it exists*

**Example body:**

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

## Actor Contact

### GET:/Organization/Actor/Contact

```organization/{organizationId:guid}/actor/{actorId:guid}/contact```<br />
*returns all contacts for the specified actor in the specified organization, if the organization and actor exists.*

### POST:/Organization/Actor/Contact

```organization/{organizationId:guid}/actor/{actorId:guid}/contact```<br />
*Creates a contact for the specified actor in the specified organization, if the organization and actor exists.*

**Example body:**

```json
{
  "name": "string",
  "category": "string",
  "email": "string",
  "phone": "string"
}
```

### DELETE:/Organization/Actor/Contact

```organization/{organizationId:guid}/contact/{contactId:guid}```<br />
*Deletes a contact from the specified actor in the specified organisation, if it exists*

## Grid Area

### GET:/GridArea

```gridarea/```<br />
*returns all grid areas.*

### POST:/GridArea

```gridarea/```<br />
*Creates a grid areas.*

**Example Body:**

```json
{
  "name": "string",
  "code": "string",
  "priceAreaCode": "string"
}
```

---

## Domain C4 model

In the DataHub 3 project we use the [C4 model](https://c4model.com/) to document the high-level software design.

The [DataHub base model](https://github.com/Energinet-DataHub/opengeh-arch-diagrams#datahub-base-model) describes elements like organizations, software systems and actors. In domain repositories we should `extend` on this model and add additional elements within the DataHub 3.0 Software System (`dh3`).

The domain C4 model and rendered diagrams are located in the folder hierarchy [docs/diagrams/c4-model](./docs/diagrams/c4-model/) and consists of:

- `model.dsl`: Structurizr DSL describing the domain C4 model.
- `views.dsl`: Structurizr DSL extending the `dh3` software system by referencing domain C4 models using `!include`, and describing the views.
- `views.json`: Structurizr layout information for views.
- `/views/*.png`: A PNG file per view described in the Structurizr DSL.

Maintenance of the C4 model should be performed using VS Code and a local version of Structurizr Lite running in Docker. See [DataHub base model](https://github.com/Energinet-DataHub/opengeh-arch-diagrams#datahub-base-model) for a description of how to do this.
