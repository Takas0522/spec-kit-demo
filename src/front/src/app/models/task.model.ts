export enum TaskStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed'
}

export interface Task {
  id: string;
  ownerId: string;
  ownerEmail: string;
  title: string;
  description?: string;
  status: string;
  dueDate?: string;
  createdAt: string;
  modifiedAt: string;
  isShared: boolean;
}

export interface CreateTaskDto {
  title: string;
  description?: string;
  dueDate?: string;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  status?: string;
  dueDate?: string;
}

export interface TaskShare {
  id: string;
  taskId: string;
  taskTitle: string;
  sharedByUserId: string;
  sharedByUserEmail: string;
  sharedWithUserId: string;
  sharedWithUserEmail: string;
  sharedAt: string;
}

export interface CreateTaskShareDto {
  sharedWithUserEmail: string;
}

export interface User {
  id: string;
  email: string;
  displayName: string;
  createdAt: string;
  lastLoginAt?: string;
}
