namespace Consumer.Domains.Queries
{
    public static class OrderQuery
    {
        public const string GET = @"
            SELECT Id,
                   IdProfile as Profile,
                   IdCountry as Country,
                   CreatedBy,
                   Name,
                   Email,
                   Document,
                   Birthdate,
                   Active
              FROM bootstrap.User
             WHERE Id = @Id
               AND CreatedBy = @CreatedBy;
        ";

        public const string UPDATE = @"
            UPDATE bootstrap.User 
               SET IdProfile = @IdProfile,
                   IdCountry = @IdCountry,
                   Name = @Name,
                   Email = @Email,
                   Birthdate = @Birthdate,
                   Active = @Active
             WHERE Id = @Id;
        ";
    }
}
