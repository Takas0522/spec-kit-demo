import { Routes } from '@angular/router';
import { TaskListComponent } from './components/tasks/task-list.component';
import { SharedTasksComponent } from './components/shared-tasks/shared-tasks.component';

export const routes: Routes = [
  { path: '', redirectTo: '/tasks', pathMatch: 'full' },
  { path: 'tasks', component: TaskListComponent },
  { path: 'shared', component: SharedTasksComponent },
];
