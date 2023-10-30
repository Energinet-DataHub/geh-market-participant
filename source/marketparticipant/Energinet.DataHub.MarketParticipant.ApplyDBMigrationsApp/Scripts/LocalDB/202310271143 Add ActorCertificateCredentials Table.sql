CREATE TABLE [dbo].[ActorCertificateCredentials]
(
    [ActorId]       [uniqueidentifier]   NOT NULL,
    [Thumbprint]    [nvarchar](40)       NOT NULL,
    [KvSecretId]    [nvarchar](128)      NOT NULL
    CONSTRAINT [UQ_ActorCertificateCredentials_Thumbprint] UNIQUE NONCLUSTERED
(
[Thumbprint] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]