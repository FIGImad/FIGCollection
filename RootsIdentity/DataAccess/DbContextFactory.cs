using FIGCommon.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace RootsIdentity.DataAccess
{
    public static class DbContextFactory
    {
        public static ApplicationDbContext Create(string connid)
        {
            if (!string.IsNullOrEmpty(connid))
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(connid);
                if (optionsBuilder.Options != null)
                {
                    return new ApplicationDbContext(optionsBuilder.Options);
                }
                throw new SqlConfigException();
            }
            else
            {
                throw new SqlConfigException();
            }
        }
    }
}
