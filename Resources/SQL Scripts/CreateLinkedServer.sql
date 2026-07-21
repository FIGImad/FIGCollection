EXEC sp_addlinkedserver 
   @server = N'RemoteDataServer', 
   @srvproduct = N'',                               -- Product name (leave blank for SQL Server)
   @provider = N'MSOLEDBSQL',                        -- Provider name (SQL Server Native Client)
   @datasrc = N'176.241.67.200,57777'        -- Remote server address
  


   EXEC sp_addlinkedsrvlogin 
   @rmtsrvname = N'RemoteDataServer', 
   @useself = N'False', 
   @locallogin = NULL, 
   @rmtuser = 'bot', 
   @rmtpassword = 'f!groot5Bot';

   --EXEC sp_dropserver @server = 'RemoteDataServer', @droplogins = 'droplogins';


     EXEC sp_testlinkedserver N'RemoteDataServer';