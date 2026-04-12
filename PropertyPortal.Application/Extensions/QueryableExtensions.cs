using PropertyPortal.Domain.Core.Interfaces;
using PropertyPortal.Domain.Entities;

namespace PropertyPortal.Application.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> SearchProperties<T>(this IQueryable<T> query, string? searchTerm)
            where T : Property // Or a common interface/base class
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return query;

            searchTerm = searchTerm.Trim();

            return query.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.Address1 != null && p.Address1.Contains(searchTerm)) ||
                (p.City != null && p.City.Contains(searchTerm)) ||
                (p.ZipCode != null && p.ZipCode.Contains(searchTerm)));
        }

        //public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string? term) where T : class //, ILocatable
        //{
        //    if (string.IsNullOrWhiteSpace(term)) return query;

        //    term = term.Trim();

        //    return query.Where(x =>
        //        x.Name.Contains(term) ||
        //         (x.Address1 != null && x.Address.Street.Contains(term)) ||
        //         (x.Address.City != null && x.Address.City.Contains(term)) ||
        //         (x.Address.ZipCode != null && x.Address.ZipCode.Contains(term)) ||
        //          x.Description != null && x.Description.Contains(term));
        //}

        public static IQueryable<T> ApplyPropertySearch<T>(this IQueryable<T> query, string? term) where T : Property
        {
            if (string.IsNullOrWhiteSpace(term)) return query;

            term = term.Trim();
            return query.Where(x =>
                x.Name.Contains(term) ||
                x.Name.Contains(term) ||
                 (x.Address1 != null && x.Address1.Contains(term)) ||
                 (x.City != null && x.City.Contains(term)) ||
                 (x.State != null && x.State.Contains(term)) ||
                 (x.ZipCode != null && x.ZipCode.Contains(term)));
        }

        public static IQueryable<T> ApplyResidentSearch<T>(this IQueryable<T> query, string? term) where T : Resident //, ILocatable
        {
            if (string.IsNullOrWhiteSpace(term)) return query;

            term = term.Trim();
            return query.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term));
                //x.Name.Contains(term) ||
                // (x.Address.Street != null && x.Address.Street.Contains(term)) ||
                // (x.Address.City != null && x.Address.City.Contains(term)) ||
                // (x.Address.ZipCode != null && x.Address.ZipCode.Contains(term)) ||
                //  x.Description != null && x.Description.Contains(term));
        }

        public static IQueryable<Lease> WhereActive(this IQueryable<Lease> query)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return query.Where(l => l.StartDate <= today && l.EndDate >= today && !l.IsDeleted);
        }
    }
}
