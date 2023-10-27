CREATE TABLE [dbo].[ActorCertificateCredentials]
(
    [Id]            [int] IDENTITY(1, 1) NOT NULL,
    [ActorId]       [uniqueidentifier]   NOT NULL,
    [Thumbprint]    [nvarchar](40)      NOT NULL,
    [KvSecretId]    [nvarchar](Max)      NOT NULL
    CONSTRAINT [PK_ActorCertificateCredentials_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
    CONSTRAINT [UQ_ActorCertificateCredentials_Thumbprint] UNIQUE NONCLUSTERED
(
[Domain] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] 