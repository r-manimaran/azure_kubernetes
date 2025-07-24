using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Models;

public class UserRecord
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }
}
