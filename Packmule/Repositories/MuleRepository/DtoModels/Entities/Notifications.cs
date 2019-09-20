using System;

namespace Packmule.Repositories.MuleRepository
{
    public partial class Notifications
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string OfficeLocation { get; set; }
        public DateTime? LastMessaged { get; set; }
    }
}
