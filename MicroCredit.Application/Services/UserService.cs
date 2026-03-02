using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Application.Model.User;
using MicroCredit.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MicroCredit.Application.Services
{
    public class UserService: IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

       
        public async Task<UserResponse?> GetorgUserAsync(int id, CancellationToken cancellationToken = default)
        {

            var users = await _unitOfWork.Users.GetAllAsync();
            var user = users.Where(u => u.Id == id).FirstOrDefault();
            if (user == null)
            {
                return null;
            }
            return user.ToUserResponse();

        }

       
        //public async Task<UserResponse?> GetBranchUserAsync(int id, CancellationToken cancellationToken = default)
        //{
        //    var user = await _unitOfWork.Users.GetByEmailAsync(id, cancellationToken);


        //}

        //public Task<UserResponse?> GetorgUserAsync(UserRequest request, CancellationToken cancellationToken = default)
        //{
        //    throw new NotImplementedException();
        //}

        public Task<UserResponse?> GetBranchUserAsync(UserRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
