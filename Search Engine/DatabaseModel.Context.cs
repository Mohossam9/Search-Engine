﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Search_Engine
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class databaseContext : DbContext
    {
        public databaseContext()
            : base("name=databaseContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Crawler> Crawlers { get; set; }
        public DbSet<Inverted_index> Inverted_index { get; set; }
        public DbSet<kgram_index> kgram_index { get; set; }
        public DbSet<Soundex_Index> Soundex_Index { get; set; }
    }
}
