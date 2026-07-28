import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  MedicalAssistantChatMessage,
  MedicalAssistantChatRequest,
  MedicalAssistantChatResponse,
} from '../models/ai-medical-assistant.model';
import { ApiResponse } from '../models/api-response.model';
import { TokenService } from './token.service';

const STORAGE_PREFIX = 'careconnect.ai-medical-assistant.v1';
const MAXIMUM_STORED_MESSAGES = 10;

@Injectable({ providedIn: 'root' })
export class AiMedicalAssistantService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenService);
  private readonly endpoint = `${environment.apiBaseUrl}/ai-medical-assistant/chat`;

  chat(request: MedicalAssistantChatRequest): Observable<MedicalAssistantChatResponse> {
    return this.http
      .post<ApiResponse<MedicalAssistantChatResponse>>(this.endpoint, request)
      .pipe(map((response) => response.data!));
  }

  loadConversation(): MedicalAssistantChatMessage[] {
    const key = this.storageKey();
    if (!key) {
      return [];
    }

    try {
      const raw = sessionStorage.getItem(key);
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) {
        return [];
      }

      return parsed
        .filter((item): item is MedicalAssistantChatMessage => this.isValidMessage(item))
        .slice(-MAXIMUM_STORED_MESSAGES);
    } catch {
      return [];
    }
  }

  saveConversation(messages: MedicalAssistantChatMessage[]): void {
    const key = this.storageKey();
    if (!key) {
      return;
    }

    try {
      sessionStorage.setItem(
        key,
        JSON.stringify(messages.slice(-MAXIMUM_STORED_MESSAGES)),
      );
    } catch {
      // The chat still remains in component memory when storage is unavailable or full.
    }
  }

  clearConversation(): void {
    const key = this.storageKey();
    if (!key) {
      return;
    }

    try {
      sessionStorage.removeItem(key);
    } catch {
      // Clearing the in-memory state is still enough for this page session.
    }
  }

  private storageKey(): string | null {
    const userId = this.tokens.user?.id;
    return userId ? `${STORAGE_PREFIX}.${userId}` : null;
  }

  private isValidMessage(value: unknown): value is MedicalAssistantChatMessage {
    if (!value || typeof value !== 'object') {
      return false;
    }

    const item = value as Partial<MedicalAssistantChatMessage>;
    return (
      typeof item.id === 'string' &&
      (item.role === 'user' || item.role === 'assistant') &&
      typeof item.content === 'string' &&
      item.content.length <= 6_000 &&
      typeof item.createdAt === 'string'
    );
  }
}
