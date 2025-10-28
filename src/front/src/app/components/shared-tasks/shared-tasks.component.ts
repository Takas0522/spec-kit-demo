import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService } from '../../services/task.service';
import { Task, TaskStatus } from '../../models/task.model';

@Component({
  selector: 'app-shared-tasks',
  imports: [CommonModule],
  templateUrl: './shared-tasks.component.html',
  styleUrls: ['./shared-tasks.component.css']
})
export class SharedTasksComponent implements OnInit {
  private taskService = inject(TaskService);
  
  sharedTasks = signal<Task[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  TaskStatus = TaskStatus;

  ngOnInit() {
    this.loadSharedTasks();
  }

  loadSharedTasks() {
    this.loading.set(true);
    this.error.set(null);
    
    this.taskService.getSharedTasks().subscribe({
      next: (tasks) => {
        this.sharedTasks.set(tasks);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load shared tasks');
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
