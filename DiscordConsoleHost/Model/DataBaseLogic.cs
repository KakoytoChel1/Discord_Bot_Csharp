using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordConsoleHost.Model
{
    internal static class DataBaseLogic
    {
        public static DbContextOptions<ApplicationContext> options;

        public static void StartSettings()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            //setting options for ApplicationContext
            options = optionsBuilder.UseSqlite("Data Source=mainDB.db").Options;
        }

        //add new item to collection
        internal static void AddNewCustomer(Customer customer)
        {
            using (var db = new ApplicationContext(options))
            {
                db.Customers.Add(customer);
                db.SaveChanges();
            }
        }

        //remove item from collection
        internal static void RemoveCustomer(Customer customer)
        {
            using(var db = new ApplicationContext(options))
            {
                db.Customers.Remove(customer);
                db.SaveChanges();
            }
        }

        //get list of the users in this collection 
        internal static Customer[] GetCustomers()
        {
            using(var db = new ApplicationContext(options))
            {
                var items = db.Customers.ToList();
                return items.ToArray();
            }
        }
    }
}
