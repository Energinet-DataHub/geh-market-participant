EXEC sp_rename 'dbo.BalanceResponsibilityAgreement', 'BalanceResponsibilityRelation'
EXEC sp_rename 'PK_BalanceResponsibilityAgreement', 'PK_BalanceResponsibilityRelation'
EXEC sp_rename 'FK_BalanceResponsibilityAgreement_EnergySupplierId_Actor', 'FK_BalanceResponsibilityRelation_EnergySupplierId_Actor'
EXEC sp_rename 'FK_BalanceResponsibilityAgreement_BalanceResponsiblePartyId_Actor', 'FK_BalanceResponsibilityRelation_BalanceResponsiblePartyId_Actor'
EXEC sp_rename 'FK_BalanceResponsibilityAgreement_GridAreaId_GridArea', 'FK_BalanceResponsibilityRelation_GridAreaId_GridArea'
