using System.Data.Entity;
using Data.Entities;
using Data.Interfaces;
using StructureMap.Configuration.DSL;

namespace Data.Infrastructure.StructureMap
{
    public class DatabaseStructureMapBootStrapper
    {
        public class CoreRegistry : Registry
        {
            public CoreRegistry()
            {
                For<IAttachmentDO>().Use<AttachmentDO>();
                For<IAttendeeDO>().Use<AttendeeDO>();
                //For<IBookingRequestDO>().Use<BookingRequestDO>();
                For<IEmailDO>().Use<EmailDO>();
                For<IEmailAddressDO>().Use<EmailAddressDO>();
                For<IUserDO>().Use<UserDO>();
                For<ICalendarDO>().Use<CalendarDO>();
                For<IAspNetRolesDO>().Use<AspNetRolesDO>();
                For<IAspNetUserRolesDO>().Use<AspNetUserRolesDO>();
                //Do not remove _ => (This gives us lazy execution, and a new unit of work & context each call). Removing this will cause the application to be unstable with threads.
                For<IUnitOfWork>().Use(_ => new UnitOfWork(_.GetInstance<IDBContext>()));
            }
        }

        public class LiveMode : CoreRegistry 
        {
            public LiveMode()
            {
                For<DbContext>().Use<DockyardDbContext>();
                For<IDBContext>().Use<DockyardDbContext>();
            }
        }

        public class TestMode : CoreRegistry
        {
            public TestMode()
            {
                For<IDBContext>().Use<MockedDBContext>();
            }
        }
    }
}