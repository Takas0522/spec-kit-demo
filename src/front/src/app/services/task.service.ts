import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Task, CreateTaskDto, UpdateTaskDto, TaskShare, CreateTaskShareDto, User } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5182/api'; // TODO: Move to environment configuration

  private getParams(userId?: string): { params?: HttpParams } {
    if (userId) {
      return { params: new HttpParams().set('userId', userId) };
    }
    return {};
  }

  // User endpoints
  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/users/me`);
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/users`);
  }

  // Task endpoints
  getTasks(userId?: string): Observable<Task[]> {
    return this.http.get<Task[]>(`${this.apiUrl}/tasks`, this.getParams(userId));
  }

  getTask(id: string, userId?: string): Observable<Task> {
    return this.http.get<Task>(`${this.apiUrl}/tasks/${id}`, this.getParams(userId));
  }

  createTask(task: CreateTaskDto, userId?: string): Observable<Task> {
    return this.http.post<Task>(`${this.apiUrl}/tasks`, task, this.getParams(userId));
  }

  updateTask(id: string, task: UpdateTaskDto, userId?: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/tasks/${id}`, task, this.getParams(userId));
  }

  deleteTask(id: string, userId?: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${id}`, this.getParams(userId));
  }

  getSharedTasks(userId?: string): Observable<Task[]> {
    return this.http.get<Task[]>(`${this.apiUrl}/tasks/shared`, this.getParams(userId));
  }

  // Task sharing endpoints
  getTaskShares(taskId: string, userId?: string): Observable<TaskShare[]> {
    return this.http.get<TaskShare[]>(`${this.apiUrl}/tasks/${taskId}/shares`, this.getParams(userId));
  }

  shareTask(taskId: string, share: CreateTaskShareDto, userId?: string): Observable<TaskShare> {
    return this.http.post<TaskShare>(`${this.apiUrl}/tasks/${taskId}/shares`, share, this.getParams(userId));
  }

  revokeShare(taskId: string, shareId: string, userId?: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${taskId}/shares/${shareId}`, this.getParams(userId));
  }
}
