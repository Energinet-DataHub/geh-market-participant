CREATE TABLE [dbo].[UsedActorCertificates]
(
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Thumbprint] [nvarchar](40) NOT NULL,
    CONSTRAINT [UQ_UsedActorCertificates_Thumbprint] UNIQUE ([Thumbprint])
)