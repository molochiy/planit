﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanIt.Entities;

namespace PlanIt.Services.Abstract
{
    public interface IUserService
    {
        User GetUserById(int id);

        User GetUserExistByEmail(string email);

        List<User> GetUsersExistByEmailSubstring(string emailSubstring);

        User AddUser(User user);
    }
}
