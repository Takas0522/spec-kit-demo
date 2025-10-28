import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../services/task.service';
import { Task, CreateTaskDto, TaskStatus } from '../../models/task.model';

@Component({
  selector: 'app-task-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.css']
})
export class TaskListComponent implements OnInit {
  private taskService = inject(TaskService);
  
  tasks = signal<Task[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  
  // Form state
  showCreateForm = signal(false);
  newTask = signal<CreateTaskDto>({ title: '', description: '', dueDate: undefined });
  
  // Edit state
  editingTaskId = signal<string | null>(null);
  editingTask = signal<Partial<Task>>({});

  TaskStatus = TaskStatus;

  ngOnInit() {
    this.loadTasks();
  }

  loadTasks() {
    this.loading.set(true);
    this.error.set(null);
    
    this.taskService.getTasks().subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load tasks');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  createTask() {
    const task = this.newTask();
    if (!task.title.trim()) {
      return;
    }

    this.loading.set(true);
    this.taskService.createTask(task).subscribe({
      next: () => {
        this.newTask.set({ title: '', description: '', dueDate: undefined });
        this.showCreateForm.set(false);
        this.loadTasks();
      },
      error: (err) => {
        this.error.set('Failed to create task');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  startEdit(task: Task) {
    this.editingTaskId.set(task.id);
    this.editingTask.set({ ...task });
  }

  cancelEdit() {
    this.editingTaskId.set(null);
    this.editingTask.set({});
  }

  saveEdit(taskId: string) {
    const updates = this.editingTask();
    
    this.loading.set(true);
    this.taskService.updateTask(taskId, {
      title: updates.title,
      description: updates.description,
      status: updates.status
    }).subscribe({
      next: () => {
        this.editingTaskId.set(null);
        this.editingTask.set({});
        this.loadTasks();
      },
      error: (err) => {
        this.error.set('Failed to update task');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  updateStatus(taskId: string, status: string) {
    this.loading.set(true);
    this.taskService.updateTask(taskId, { status }).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        this.error.set('Failed to update task status');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  deleteTask(taskId: string) {
    if (!confirm('このタスクを削除してもよろしいですか？')) {
      return;
    }

    this.loading.set(true);
    this.taskService.deleteTask(taskId).subscribe({
      next: () => {
        this.loadTasks();
      },
      error: (err) => {
        this.error.set('Failed to delete task');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case TaskStatus.NotStarted:
        return 'bg-gray-100 text-gray-800';
      case TaskStatus.InProgress:
        return 'bg-blue-100 text-blue-800';
      case TaskStatus.Completed:
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case TaskStatus.NotStarted:
        return '未実施';
      case TaskStatus.InProgress:
        return '実施中';
      case TaskStatus.Completed:
        return '完了';
      default:
        return status;
    }
  }
}
