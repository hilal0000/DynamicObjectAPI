﻿using DynamicObjectAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DynamicObjectAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<DynamicObject> DynamicObjects { get; set; }
        public DbSet<DynamicObjectTypes> DynamicObjectTypes { get; set; } 

    }
}
