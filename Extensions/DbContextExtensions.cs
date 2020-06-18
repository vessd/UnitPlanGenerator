using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Extensions
{
    public static class DbContextExtensions
    {
        public static async Task<T> UpsertAsync<T>(this DbContext context, T item) where T : class, IBaseModel
        {
            var set = context.Set<T>();
            
            set.Attach(item);

            if (context.Entry(item).State == EntityState.Unchanged)
            {
                context.Entry(item).State = EntityState.Modified;
            }

            await context.SaveChangesAsync();

            return item;
        }

        public static async Task<Title> UpsertAsync(this DbContext context, Title title)
        {
            var set = context.Set<Title>();

            var titleById = await set.FindAsync(title.Id);
            var titleByValue = await set.FirstOrDefaultAsync(t => t.Value == title.Value);

            if (titleByValue == null)
            {
                if (titleById == null)
                {
                    set.Add(title);
                }
                else
                {
                    if (titleById.SubjectSets.Count > 1)
                    {
                        title =  new Title { Value = title.Value };
                        set.Add(title);
                    }
                    else
                    {
                        titleById.Value = title.Value;
                    }
                }
            }
            else
            {
                title = titleByValue;
            }

            await context.SaveChangesAsync();

            return title;
        }

        public static async Task DeleteAsync<T>(this DbContext context, T item) where T : class, IBaseModel
        {
            var set = context.Set<T>();
            var current = await set.FindAsync(item.Id);
            if (current != null)
            {
                set.Remove(current);
                await context.SaveChangesAsync();
            }
        }

        public static async Task DeleteAsync(this DbContext context, User user)
        {
            var set = context.Set<User>();
            var current = await set.FindAsync(user.Id);
            if (current != null)
            {
                foreach (var semester in current.Semesters)
                {
                    semester.User = null;
                }
                set.Remove(current);
                await context.SaveChangesAsync();
            }
        }
    }
}
