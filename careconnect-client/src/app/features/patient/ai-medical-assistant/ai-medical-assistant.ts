import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  MedicalAssistantChatMessage,
  MedicalAssistantChatRequest,
  MedicalAssistantChatResponse,
} from '../../../core/models/ai-medical-assistant.model';
import { AiMedicalAssistantService } from '../../../core/services/ai-medical-assistant.service';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

const MAXIMUM_MESSAGE_CHARACTERS = 2_000;
const MAXIMUM_HISTORY_MESSAGES = 10;

@Component({
  selector: 'app-ai-medical-assistant',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './ai-medical-assistant.html',
  styleUrl: './ai-medical-assistant.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiMedicalAssistantPage implements OnInit {
  private readonly assistant = inject(AiMedicalAssistantService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  @ViewChild('chatViewport')
  private chatViewport?: ElementRef<HTMLElement>;

  protected readonly maximumCharacters = MAXIMUM_MESSAGE_CHARACTERS;
  protected readonly messageControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(MAXIMUM_MESSAGE_CHARACTERS)],
  });

  protected readonly messages = signal<MedicalAssistantChatMessage[]>([]);
  protected readonly sending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly lastFailedRequest = signal<MedicalAssistantChatRequest | null>(null);

  protected readonly starterQuestions = [
    'Which specialty should I visit for recurring headaches?',
    'What are common causes of stomach pain?',
    'When should a fever require urgent medical care?',
    'أروح لدكتور تخصص إيه لو عندي صداع متكرر؟',
    'إيه الأسباب الشائعة لألم المعدة؟',
    'إمتى ارتفاع الحرارة يحتاج رعاية عاجلة؟',
  ];

  ngOnInit(): void {
    this.messages.set(this.assistant.loadConversation());
    this.scrollToNewest();
  }

  protected send(): void {
    const message = this.messageControl.value.trim();
    if (!message || this.messageControl.invalid || this.sending()) {
      this.messageControl.markAsTouched();
      return;
    }

    const request: MedicalAssistantChatRequest = {
      message,
      history: this.messages()
        .slice(-MAXIMUM_HISTORY_MESSAGES)
        .map((item) => ({ role: item.role, content: item.content })),
    };

    this.appendMessage({
      id: this.createMessageId(),
      role: 'user',
      content: message,
      createdAt: new Date().toISOString(),
    });

    this.messageControl.reset('');
    this.executeRequest(request);
  }

  protected retry(): void {
    const request = this.lastFailedRequest();
    if (request && !this.sending()) {
      this.executeRequest(request);
    }
  }

  protected useStarter(question: string): void {
    this.messageControl.setValue(question);
    this.send();
  }

  protected onComposerKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  protected clearChat(): void {
    if (this.messages().length === 0) {
      return;
    }

    const data: ConfirmDialogData = {
      title: 'Clear medical assistant chat?',
      message:
        'This removes the current conversation from this browser session. It cannot be restored.',
      confirmLabel: 'Clear chat',
      destructive: true,
    };

    this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.messages.set([]);
        this.errorMessage.set(null);
        this.lastFailedRequest.set(null);
        this.assistant.clearConversation();
      });
  }

  protected browseSpecialty(specialtyId: string): void {
    void this.router.navigate(['/doctors'], {
      queryParams: { specialtyId },
    });
  }

  protected directionFor(text: string): 'rtl' | 'ltr' {
    return /[\u0600-\u06ff]/.test(text) ? 'rtl' : 'ltr';
  }

  private executeRequest(request: MedicalAssistantChatRequest): void {
    this.sending.set(true);
    this.errorMessage.set(null);
    this.lastFailedRequest.set(null);
    this.scrollToNewest();

    this.assistant
      .chat(request)
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: (response) => {
          this.appendAssistantResponse(response);
          this.scrollToNewest();
        },
        error: (error: unknown) => {
          this.errorMessage.set(
            friendlyMessageOf(
              error,
              'The medical assistant is temporarily unavailable. Please try again later.',
            ),
          );
          this.lastFailedRequest.set(request);
          this.scrollToNewest();
        },
      });
  }

  private appendAssistantResponse(response: MedicalAssistantChatResponse): void {
    this.appendMessage({
      id: this.createMessageId(),
      role: 'assistant',
      content: response.answer,
      createdAt: new Date().toISOString(),
      urgencyLevel: response.urgencyLevel,
      suggestedSpecialtyId: response.suggestedSpecialtyId,
      suggestedSpecialtyName: response.suggestedSpecialtyName,
      redFlags: response.redFlags,
      disclaimer: response.disclaimer,
    });
  }

  private appendMessage(message: MedicalAssistantChatMessage): void {
    const next = [...this.messages(), message].slice(-MAXIMUM_HISTORY_MESSAGES);
    this.messages.set(next);
    this.assistant.saveConversation(next);
    this.scrollToNewest();
  }

  private scrollToNewest(): void {
    setTimeout(() => {
      const element = this.chatViewport?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }

  private createMessageId(): string {
    return globalThis.crypto?.randomUUID?.() ??
      `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  }
}
