ALTER ROLE [db_owner] ADD MEMBER [aer-admin];
GO

ALTER ROLE [db_ddladmin] ADD MEMBER [aer-admin];
GO

ALTER ROLE [db_denydatawriter] ADD MEMBER [aer-reader];
GO

ALTER ROLE [db_datareader] ADD MEMBER [aer-reader];
GO

ALTER ROLE [db_datawriter] ADD MEMBER [aer-writer];
GO

ALTER ROLE [db_datareader] ADD MEMBER [aer-writer];
GO
