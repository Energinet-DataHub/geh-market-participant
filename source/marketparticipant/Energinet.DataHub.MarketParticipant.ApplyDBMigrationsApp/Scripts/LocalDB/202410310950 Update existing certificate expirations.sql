UPDATE [dbo].[ActorCertificateCredentials]
SET [ExpirationDate] = DATEADD(dd, 360, GETUTCDATE())
WHERE [ExpirationDate] > DATEADD(dd, 360, GETUTCDATE())