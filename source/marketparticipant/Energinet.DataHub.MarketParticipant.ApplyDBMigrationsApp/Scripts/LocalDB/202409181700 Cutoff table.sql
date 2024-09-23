CREATE TABLE Cutoff
(
    Type      INT PRIMARY KEY,
    Timestamp DATETIMEOFFSET NOT NULL,
    Version   ROWVERSION
);