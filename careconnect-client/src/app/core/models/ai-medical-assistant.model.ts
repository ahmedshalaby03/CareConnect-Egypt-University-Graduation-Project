export type MedicalUrgencyLevel = 'Routine' | 'Urgent' | 'Emergency';
export type MedicalAssistantRole = 'user' | 'assistant';

export interface MedicalAssistantHistoryItem {
  role: MedicalAssistantRole;
  content: string;
}

export interface MedicalAssistantChatRequest {
  message: string;
  history: MedicalAssistantHistoryItem[];
}

export interface MedicalAssistantChatResponse {
  answer: string;
  urgencyLevel: MedicalUrgencyLevel;
  suggestedSpecialtyId: string | null;
  suggestedSpecialtyName: string | null;
  redFlags: string[];
  disclaimer: string;
}

/** Browser-session-only shape. It is never sent to SQL Server. */
export interface MedicalAssistantChatMessage {
  id: string;
  role: MedicalAssistantRole;
  content: string;
  createdAt: string;
  urgencyLevel?: MedicalUrgencyLevel;
  suggestedSpecialtyId?: string | null;
  suggestedSpecialtyName?: string | null;
  redFlags?: string[];
  disclaimer?: string;
}
