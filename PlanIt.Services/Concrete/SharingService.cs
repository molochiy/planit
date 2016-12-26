﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanIt.Entities;
using PlanIt.Repositories.Abstract;
using PlanIt.Services.Abstract;

namespace PlanIt.Services.Concrete
{
    public class SharingService : BaseService, ISharingService
    {
        public SharingService(IRepository repository) : base(repository)
        {
        }

        /// <summary>
        /// Sharing own plan to another user
        /// </summary>
        /// <param name="planId">Current plan id</param>
        /// <param name="fromUserEmail">User who shares own plan</param>
        /// <param name="toUserEmail">User who receives shared plan</param>
        public void SharePlan(int planId, string fromUserEmail, string toUserEmail)
        {
            //Add check if current plan exists, owner exists, receiver exists
            //Check whether current plan isn't already shared to htis user
            var sharingDateTime = DateTime.Now;
            var fromUserId = _repository.GetSingle<User>(u => u.Email == fromUserEmail).Id;
            var toUserId = _repository.GetSingle<User>(u => u.Email == toUserEmail).Id;
            var sharingStatusId = _repository.GetSingle<SharingStatus>(ss => ss.Name == "Pending").Id;
            var sharedPlanUser = new SharedPlanUser
            {
                PlanId = planId,
                SharingDateTime = sharingDateTime,
                SharingStatusId = sharingStatusId,
                UserOwnerId = fromUserId,
                UserReceiverId = toUserId
            };
            _repository.Insert(sharedPlanUser);
        }

        /// <summary>
        /// Getting information about sharing (from table SharedPlanUser) for current user
        /// to notify user about such events:
        /// 1) someone shared plan with current user
        /// 2) someone accepted or declined plan current user shared
        /// </summary>
        /// <param name="userEmail">Email of current user</param>
        /// <returns>Container of sharing info</returns>
        public List<SharedPlanUser> GetSharingInfoForNotifications(string userEmail)
        {
            //Check whether user exists
            var userId = _repository.GetSingle<User>(u => u.Email == userEmail).Id;
            var pandingStatusId = _repository.GetSingle<SharingStatus>(s => s.Name == "Pending").Id;
            var acceptedStatusId = _repository.GetSingle<SharingStatus>(s => s.Name == "Accepted").Id;
            var declinedStatusId = _repository.GetSingle<SharingStatus>(s => s.Name == "Declined").Id;
            return _repository.Get<SharedPlanUser>(
               (s => 
                    (s.UserReceiverId == userId && s.SharingStatusId == pandingStatusId) ||
                    (s.UserOwnerId == userId && s.OwnerWasNotified == false &&
                                         (s.SharingStatusId == acceptedStatusId ||
                                          s.SharingStatusId == declinedStatusId))));
        }

        /// <summary>
        /// Getting number of notifications for current user
        /// </summary>
        /// <param name="userEmail">Email of current user</param>
        /// <returns>Number of notifications</returns>
        public int GetNumberOfNotifications(string userEmail)
        {
            return GetSharingInfoForNotifications(userEmail).Count();
        }

        /// <summary>
        /// Change sharing status for some sharing info (in table SharedPlanUser)
        /// </summary>
        /// <param name="sharedPlanUserId">Sharing info id</param>
        /// <param name="newSharingStatus">New sharing status string</param>
        public void ChangeSharingStatus(int sharedPlanUserId, string newSharingStatus)
        {
            //Check whether status i correct (Accepted, Declined or Pending and nothing else)
            var sharedInfo = _repository.GetSingle<SharedPlanUser>(s => s.Id == sharedPlanUserId);
            var statusId = _repository.GetSingle<SharingStatus>(s => s.Name == newSharingStatus).Id;
            sharedInfo.SharingStatusId = statusId;
            sharedInfo.SharingDateTime = DateTime.Now;
            _repository.Update<SharedPlanUser>(sharedInfo);
        }

        /// <summary>
        /// When ownre was notified appropriate property is setted with true
        /// </summary>
        /// <param name="sharedPlanUserId">Current sharing info id</param>
        /// <param name="newValue">True</param>
        public void ChangeOwnerWasNotifiedProperty(int sharedPlanUserId, bool newValue)
        {
            //Check whether such sharing info exists
            var sharedInfo = _repository.GetSingle<SharedPlanUser>(s => s.Id == sharedPlanUserId);
            sharedInfo.OwnerWasNotified = newValue;
            _repository.Update<SharedPlanUser>(sharedInfo);
        }

        public string GetSharingStatusById(int sharingStatusId)
        {
            return _repository.GetSingle<SharingStatus>(s => s.Id == sharingStatusId).Name;
        }

        public List<string> GetUsersEmailsForNotification(int sharedPlanUserId, string newStatus)
        {
            var userEmails = new List<string>();

            SharedPlanUser sharedPlanUser = _repository.GetSingle<SharedPlanUser>(spu => spu.Id == sharedPlanUserId);

            if (sharedPlanUser != null)
            {
                User userOwner = _repository.GetSingle<User>(u => u.Id == sharedPlanUser.UserOwnerId);

                if (userOwner != null)
                {
                    userEmails.Add(userOwner.Email);
                }

                if (newStatus != "Notified")
                {
                    User userReceiver = _repository.GetSingle<User>(u => u.Id == sharedPlanUser.UserReceiverId);

                    if (userReceiver != null)
                    {
                        userEmails.Add(userReceiver.Email);
                    }
                }
            }

            return userEmails;
        }

        /// <summary>
        /// Get emails of users who should see comment for current plan
        /// </summary>
        /// <param name="planId">Current plan id</param>
        /// <returns>List of emails</returns>
        public List<string> GetUsersEmailsWhoShouldGetComment(int planId)
        {        
            int acceptedSharingStatusId = _repository.GetSingle<SharingStatus>(s => s.Name == "Accepted").Id;

            //get receivers
            List<int> usersId = _repository.Get<SharedPlanUser>(s => s.PlanId == planId && s.SharingStatusId == acceptedSharingStatusId).Select(s => s.UserReceiverId).ToList();
            List<string> users = _repository.Get<User>(u => usersId.Contains(u.Id)).Select(u => u.Email).ToList();

            //add owner
            int ownerId = _repository.GetSingle<Plan>(p => p.Id == planId).UserId;
            users.Add(_repository.GetSingle<User>(u => u.Id == ownerId).Email);

            return users;
        }
    }
}
