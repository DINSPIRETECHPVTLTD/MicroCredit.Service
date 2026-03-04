using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class MembersService : IMembersService
    {
        // Simple in-memory store for testing
        private static readonly List<MemberResponse> _store = new();
        private static int _nextId = 1;
        private static readonly object _lock = new();

        public MembersService()
        {
        }

        public Task<IEnumerable<MemberResponse>> GetMembersAsync(int branchId, CancellationToken cancellationToken = default)
        {
            // For testing ignore branchId and return all
            IEnumerable<MemberResponse> result;
            lock (_lock)
            {
                result = _store.Select(m => new MemberResponse
                {
                    Id = m.Id,
                    FirstName = m.FirstName,
                    SurName = m.SurName,
                    PhoneNumber = m.PhoneNumber
                }).ToList();
            }
            return Task.FromResult(result);
        }

        public Task<MemberResponse?> GetMemberAsync(int id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var m = _store.FirstOrDefault(x => x.Id == id);
                return Task.FromResult(m);
            }
        }

        public Task<MemberResponse> CreateMemberAsync(MemberRequest request, CancellationToken cancellationToken = default)
        {
            MemberResponse created;
            lock (_lock)
            {
                var id = _nextId++;
                created = new MemberResponse
                {
                    Id = id,
                    FirstName = request.FirstName,
                    SurName = request.SurName,
                    PhoneNumber = request.PhoneNumber
                };
                _store.Add(created);
            }
            return Task.FromResult(created);
        }

        public Task<MemberResponse> UpdateMemberAsync(int id, MemberRequest request, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var existing = _store.FirstOrDefault(x => x.Id == id);
                if (existing == null)
                    throw new KeyNotFoundException($"Member with id {id} not found");

                existing.FirstName = request.FirstName;
                existing.SurName = request.SurName;
                existing.PhoneNumber = request.PhoneNumber;

                return Task.FromResult(existing);
            }
        }
    }
}


