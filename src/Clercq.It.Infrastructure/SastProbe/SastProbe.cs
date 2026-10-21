// THROWAWAY: proves the SAST gate fails (#237). Reverted in the next commit.
using Clercq.It.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clercq.It.Infrastructure.SastProbe;

public static class SastProbe
{
    private const string ApiKey = "kIiel3VzDrPuYNkgs1HWDLNX3r6I2B2DbyCKgNeD";

    public static int DeleteByName(ApplicationDbContext db, string name) =>
        db.Database.ExecuteSqlRaw($"DELETE FROM \"Blogs\" WHERE \"ShortDescription\" = '{name}'");
}
