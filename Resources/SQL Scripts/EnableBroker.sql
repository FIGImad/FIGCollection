
USE master;
GO
ALTER DATABASE FIGAutoTrade SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE FIGAutoTrade SET ENABLE_BROKER;
ALTER DATABASE FIGAutoTrade SET MULTI_USER;
ALTER DATABASE FIGBroker SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE FIGBroker SET ENABLE_BROKER;
ALTER DATABASE FIGBroker SET MULTI_USER;
ALTER DATABASE FIGMonitor SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE FIGMonitor SET ENABLE_BROKER;
ALTER DATABASE FIGMonitor SET MULTI_USER;
GO

USE FIGAutoTrade;
GO
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO roots;
GO
USE FIGBroker;
GO
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO roots;
GO
USE FIGMonitor;
GO
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO roots;
GO
USE master;
GO
ALTER DATABASE FIGAutoTrade SET TRUSTWORTHY ON;
ALTER DATABASE FIGBroker SET TRUSTWORTHY ON;
ALTER DATABASE FIGMonitor SET TRUSTWORTHY ON;
GO
SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'FIGAutoTrade' or name='FIGBroker' or name = 'FIGMonitor';
GO
-- TO Check Query Notification Subscriptions
-------------------------------------------------
SELECT 
    dp.state_desc AS PermissionState,
    dp.permission_name AS Permission,
    USER_NAME(dp.grantee_principal_id) AS Grantee,
    USER_NAME(dp.grantor_principal_id) AS Grantor
FROM 
    [FIGAutoTrade].sys.database_permissions dp
WHERE 
    dp.permission_name = 'SUBSCRIBE QUERY NOTIFICATIONS'
    AND dp.class_desc = 'DATABASE';

SELECT 
    dp.state_desc AS PermissionState,
    dp.permission_name AS Permission,
    USER_NAME(dp.grantee_principal_id) AS Grantee,
    USER_NAME(dp.grantor_principal_id) AS Grantor
FROM 
    [FIGBroker].sys.database_permissions dp
WHERE 
    dp.permission_name = 'SUBSCRIBE QUERY NOTIFICATIONS'
    AND dp.class_desc = 'DATABASE';

SELECT 
    dp.state_desc AS PermissionState,
    dp.permission_name AS Permission,
    USER_NAME(dp.grantee_principal_id) AS Grantee,
    USER_NAME(dp.grantor_principal_id) AS Grantor
FROM 
    [FIGMonitor].sys.database_permissions dp
WHERE 
    dp.permission_name = 'SUBSCRIBE QUERY NOTIFICATIONS'
    AND dp.class_desc = 'DATABASE';