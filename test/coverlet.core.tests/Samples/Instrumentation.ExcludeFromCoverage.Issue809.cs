// Remember to use full name because adding new using directives change line numbers
using System.Linq;

namespace Coverlet.Core.Samples.Tests
{

    public class ParentTask_Issue809
    {
        public int Parent_ID { get; set; }
        public int Parent_Task { get; set; }
        public string ParentTaskDescription { get; set; }
        public System.Collections.Generic.List<Tasks_Issue809> Tasks { get; set; }
    }

    public class Tasks_Issue809
    {
        public int TaskId { get; set; }
        public string TaskDeatails { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public int ParentTaskId { get; set; }
        public int Priortiy { get; set; }
        public int Status { get; set; }

        public ParentTask_Issue809 ParentTask { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TaskContext_Issue809
    {
        public virtual System.Collections.Generic.List<ParentTask_Issue809> ParentTasks { get; set; }
        public virtual System.Collections.Generic.List<Tasks_Issue809> Tasks { get; set; }

        internal System.Threading.Tasks.Task<int> SaveChangesAsync()
        {
            throw new System.NotImplementedException();
        }

        internal void Update<T>(T tasks)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SearchMsg_Issue809
    {
        public int TaskId { get; set; } = -1;
        public string TaskDescription { get; set; }
        public int ParentTaskId { get; set; } = -1;
        public int PriorityFrom { get; set; } = -1;
        public int PriorityTo { get; set; } = -1;
        public System.DateTime FromDate { get; set; }
        public System.DateTime ToDate { get; set; }
    }

    public class TaskRepo_Issue809
    {
        private readonly TaskContext_Issue809 taskContext = new TaskContext_Issue809();

        public System.Collections.Generic.List<Tasks_Issue809> GetTaskForAllCriteria(SearchMsg_Issue809 searchMsg)
        {
            var criteriaPredicate = LinqKit.PredicateBuilder.New<Tasks_Issue809>(true);
            if (searchMsg.TaskId > 0)
                criteriaPredicate = criteriaPredicate.And(tsk => tsk.TaskId == searchMsg.TaskId);
            if (searchMsg.ParentTaskId > 0)
            {
                var parentTask = taskContext.ParentTasks.FirstOrDefault(
                   partask => partask.Parent_Task == searchMsg.ParentTaskId);
                var parentId = (parentTask != default) ? parentTask.Parent_ID : 0;

                criteriaPredicate = criteriaPredicate.And(tsk => tsk.ParentTaskId == parentId);
            }

            if (searchMsg.PriorityFrom > 0)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.Priortiy >= searchMsg.PriorityFrom);
            if (searchMsg.PriorityTo > 0)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.Priortiy <= searchMsg.PriorityTo);
            if (searchMsg.FromDate > System.DateTime.MinValue)
                criteriaPredicate = criteriaPredicate.And(tsk => tsk.StartDate == searchMsg.FromDate);
            if (searchMsg.ToDate > System.DateTime.MinValue)
                criteriaPredicate = criteriaPredicate.And(tsk => tsk.EndDate == searchMsg.ToDate);
            if (!string.IsNullOrWhiteSpace(searchMsg.TaskDescription))
                criteriaPredicate = criteriaPredicate.And(tsk =>
                tsk.TaskDeatails.CompareTo(searchMsg.TaskDescription) == 0);

            var anyTaskQuery = from taskEntity in taskContext.Tasks.Where(criteriaPredicate.Compile())
                               select taskEntity;

            var tasks = anyTaskQuery.ToList();
            tasks.ForEach(task =>
            {
                if (task.ParentTaskId > 0)
                {
                    task.ParentTask = taskContext.ParentTasks.FirstOrDefault();

                }
            });

            return tasks;

        }

        public System.Collections.Generic.List<Tasks_Issue809> GetTaskForAnyCriteria(SearchMsg_Issue809 searchMsg)
        {
            var criteriaPredicate = LinqKit.PredicateBuilder.New<Tasks_Issue809>(false);
            if (searchMsg.TaskId > 0)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.TaskId == searchMsg.TaskId);
            if (!string.IsNullOrWhiteSpace(searchMsg.TaskDescription))
                criteriaPredicate = criteriaPredicate.Or(tsk =>
                tsk.TaskDeatails.CompareTo(searchMsg.TaskDescription) == 0);
            if (searchMsg.ParentTaskId > 0)
            {
                var parentTask = taskContext.ParentTasks.FirstOrDefault(
                    partask => partask.Parent_Task == searchMsg.ParentTaskId);
                var parentId = (parentTask != default) ? parentTask.Parent_ID : 0;

                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.ParentTaskId == parentId);
            }

            if (searchMsg.PriorityFrom > 0)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.Priortiy >= searchMsg.PriorityFrom);
            if (searchMsg.PriorityTo > 0)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.Priortiy <= searchMsg.PriorityTo);
            if (searchMsg.FromDate > System.DateTime.MinValue)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.StartDate == searchMsg.FromDate);
            if (searchMsg.ToDate > System.DateTime.MinValue)
                criteriaPredicate = criteriaPredicate.Or(tsk => tsk.EndDate == searchMsg.ToDate);
            var anyTaskQuery = from taskEntity in taskContext.Tasks.Where(criteriaPredicate.Compile())
                               select taskEntity;

            var tasks = anyTaskQuery.ToList();
            tasks.ForEach(task =>
            {
                if (task.ParentTaskId > 0)
                {
                    task.ParentTask = taskContext.ParentTasks.FirstOrDefault();
                }
            });

            return tasks;
        }
        public async System.Threading.Tasks.Task<bool> AddTask(Tasks_Issue809 tasks)
        {
            _ = await manageParentTask(tasks);
            taskContext.Tasks.Add(tasks);
            var rowsAffected = await taskContext.SaveChangesAsync();
            return (rowsAffected > 0) ? true : false;

        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async System.Threading.Tasks.Task<bool> EditTask(Tasks_Issue809 tasks, int val)
        {
            if (val == 10)
            {
                return true;
            }
            else
            {
                if ((tasks.ParentTask != default) && (tasks.ParentTask.Parent_Task == 0))
                    tasks.ParentTask = null;

                var oldTaskQuery = from taskEntity in taskContext.Tasks.Where(tsk => tsk.TaskId == tasks.TaskId)
                                   from parTaskEntity in taskContext.ParentTasks.Where(partask =>
                                   partask.Parent_ID == taskEntity.ParentTaskId).DefaultIfEmpty()
                                   select new { taskEntity, parTaskEntity };
                var oldTaskValueObj = oldTaskQuery.FirstOrDefault();
                var oldTask = oldTaskValueObj.taskEntity;
                if (oldTaskValueObj.parTaskEntity != default)
                {
                    oldTask.ParentTask = new ParentTask_Issue809
                    {
                        Parent_ID = oldTaskValueObj.parTaskEntity.Parent_ID,
                        ParentTaskDescription = oldTaskValueObj.parTaskEntity.ParentTaskDescription,
                        Parent_Task = oldTaskValueObj.parTaskEntity.Parent_Task
                    };
                }


                if (oldTask == default)
                    throw new System.ApplicationException("Task not found");
                if (((oldTask.ParentTask != null) && (oldTask.ParentTask.Parent_ID != tasks.ParentTaskId)) ||
                    ((oldTask.ParentTask == default) && (tasks.ParentTask != default) && (tasks.ParentTask.Parent_Task > 0)))
                    _ = await manageParentTask(tasks);


                taskContext.Update<Tasks_Issue809>(tasks);
                var rowsAffected = await taskContext.SaveChangesAsync();

                bool combinedResult = (rowsAffected > 0) ? true : false;
                bool parentUpdateResult = await UpdateParentTakDetails(tasks);
                if ((combinedResult) && (parentUpdateResult))
                    return true;
                else
                    return false;
            }
        }

        private async System.Threading.Tasks.Task<bool> UpdateParentTakDetails(Tasks_Issue809 task)
        {
            var parentTask = taskContext.ParentTasks.FirstOrDefault(parTsk =>
                                                            parTsk.Parent_Task == task.ParentTaskId);
            if ((parentTask != default) &&
                (parentTask.ParentTaskDescription.CompareTo(task.TaskDeatails) != 0))
            {
                parentTask.ParentTaskDescription = task.TaskDeatails;
                taskContext.Update(parentTask);
                var recordsAffected = await taskContext.SaveChangesAsync();
                return (recordsAffected > 0) ? true : false;
            }
            return true;

        }

        private async System.Threading.Tasks.Task<Tasks_Issue809> manageParentTask(Tasks_Issue809 task)
        {

            if ((task.ParentTask != null) && (task.ParentTask.Parent_Task > 0))
            {
                ParentTask_Issue809 parentTask = taskContext.ParentTasks.FirstOrDefault(parTsk =>
                                                            parTsk.Parent_Task == task.ParentTaskId);
                if (parentTask == default)
                {
                    var parTaskFromTaskEntity = taskContext.Tasks
                        .FirstOrDefault(tsk => tsk.TaskId == task.ParentTaskId);
                    parentTask = new ParentTask_Issue809
                    {
                        Parent_Task = parTaskFromTaskEntity.TaskId,
                        ParentTaskDescription = parTaskFromTaskEntity.TaskDeatails
                    };
                    taskContext.ParentTasks.Add(parentTask);
                    await taskContext.SaveChangesAsync();

                }
                else
                {
                    taskContext.Update(parentTask);
                    await taskContext.SaveChangesAsync();

                }

                task.ParentTaskId = parentTask.Parent_ID;
                task.ParentTask = parentTask;
            }
            else
                task.ParentTask = null;

            return task;
        }

        public System.Threading.Tasks.Task<System.Collections.Generic.List<ParentTask_Issue809>> GetAllParentTasks()
        {
            return System.Threading.Tasks.Task.FromResult(taskContext.ParentTasks.ToList());
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public System.Collections.Generic.List<Tasks_Issue809> GetAllTasks()
        {
            var taskQuery = from taskEntity in taskContext.Tasks.Where(tsk => tsk.Status >= 0)
                            from parTaskEntity in taskContext.ParentTasks.Where(partask =>
                            partask.Parent_ID == taskEntity.ParentTaskId).DefaultIfEmpty()
                            select new { taskEntity, parTaskEntity };
            var taskValueObj = taskQuery.ToList();
            var tasks = taskValueObj.Select(valueObj =>
            {
                if (valueObj.parTaskEntity != null)
                {
                    valueObj.taskEntity.ParentTask = valueObj.parTaskEntity;
                }
                return valueObj.taskEntity;
            }).ToList();
            return tasks;
        }
    }
}