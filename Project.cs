using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPlans_by_WFP
{
    public class Project
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public List<TaskModel> Tasks { get; set; }

        public Project()
        {
            Tasks = new List<TaskModel>();
        }
    }
}
