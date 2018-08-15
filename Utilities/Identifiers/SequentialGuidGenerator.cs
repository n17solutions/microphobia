using System;
using N17Solutions.Microphobia.ServiceContract.Enums;

namespace N17Solutions.Microphobia.Utilities.Identifiers
{
    public static class SequentialGuidGenerator
    {
        public static Guid Generate(Storage storageType)
        {
            switch (storageType)
            {
                case Storage.Postgres:
                    return RT.Comb.Provider.PostgreSql.Create();
                case Storage.SqlServer:
                    return RT.Comb.Provider.Sql.Create();
                case Storage.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(storageType), storageType, null);
            }

            return RT.Comb.Provider.Legacy.Create();
        }
    }
}