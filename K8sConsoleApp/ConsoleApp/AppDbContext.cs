using FileProcessingApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp;

public class AppDbContext: DbContext
{
    public AppDbContext()
    {
        
    }
    public DbSet<Product> Products { get; set; }
}
