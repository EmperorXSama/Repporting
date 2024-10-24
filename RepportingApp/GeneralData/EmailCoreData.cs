using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataAccess.Enums;
using DataAccess.Models;

namespace RepportingApp.GeneralData;

public  static class EmailCoreData
{
    public static ObservableCollection<EmailsCoreModel> GetEmailCoreData()
    {
        return
      new ObservableCollection<EmailsCoreModel>
        {
            new EmailsCoreModel { 
                EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", 
                Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                }, 
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.New,
                IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-34) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                },
            },
            new EmailsCoreModel { EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                }, 
                IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-2) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddHours(-23) },
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.New,  },
            new EmailsCoreModel { EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-1) },
                    new IdCollectionFrequency { Id = 3, CollectedTime = DateTime.UtcNow }
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.New,},
            new EmailsCoreModel { EmailAddress = "5alidg19@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-1) },
                },
                Proxy = "129.0.0.1", Port = "8084", NumSpam = 470, Status = Status.New,},
            new EmailsCoreModel { EmailAddress = "5alidg29@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-5) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-2) },
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.Blocked,  },
            new EmailsCoreModel { EmailAddress = "5alidg59@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "B"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-1) },
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.Active,  },
            new EmailsCoreModel { EmailAddress = "5alidg50@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-1) },
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.New,  },
            new EmailsCoreModel { EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "B"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-3) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddDays(-1) },
                },
                Proxy = "129.0.0.1", Port = "8083", NumSpam = 470, Status = Status.Active,  },
            new EmailsCoreModel { EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "A"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddDays(-2) },
                    new IdCollectionFrequency { Id = 2, CollectedTime = DateTime.UtcNow.AddHours(-3) },
                    new IdCollectionFrequency { Id = 3, CollectedTime = DateTime.UtcNow.AddHours(-2) }
                },
                Proxy = "129.0.0.1", Port = "8084", NumSpam = 470, Status = Status.Old,  },
            new EmailsCoreModel { EmailAddress = "5alidg39@gmail.com", MailBox = "N/A", Group =  new GroupModel()
                {
                    Id = 1,
                    GroupName = "C"
                },  IdsFrequencies = new List<IdCollectionFrequency>
                {
                    new IdCollectionFrequency { Id = 1, CollectedTime = DateTime.UtcNow.AddHours(-3) },
                    new IdCollectionFrequency { Id = 3, CollectedTime = DateTime.UtcNow.AddHours(-1)}
                },
                Proxy = "129.0.0.1", Port = "8081", NumSpam = 470, Status = Status.New,  },
        };
    }
}