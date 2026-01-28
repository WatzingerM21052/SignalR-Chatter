import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

// Interface für Chat-Nachrichten für die Anzeige
interface ChatMessage {
  timestamp: string;
  name: string;
  message: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html', // Achte darauf, dass du app.html entsprechend anpasst
  styleUrl: './app.scss'
})
export class App implements OnInit {
  // Login Daten
  name = 'Hansi';
  password = 'abcdefg';
  isSignedIn = false;
  isAdmin = false;

  // Chat Daten
  currentMessage = '';
  currentTopic = '';
  messages: ChatMessage[] = [];

  // Extension: Topics
  topicsOfInterestInput = '';
  topicsOfInterest: string[] = [];

  // Admin Daten
  nrClients = 0;
  adminNotifications: string[] = [];

  private hubConnection: HubConnection;

  constructor() {
    // Verbindung aufbauen
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/hub/chat')
      .build();
  }

  ngOnInit() {
    this.startConnection();
    this.registerHandlers();
  }

  private startConnection() {
    this.hubConnection
      .start()
      .then(() => console.log('Connection started'))
      .catch(err => console.log('Error while starting connection: ' + err));
  }

  private registerHandlers() {
    // Empfang: NewMessage
    this.hubConnection.on('NewMessage', (name: string, message: string, timestamp: string) => {
      this.messages.push({ name, message, timestamp });
    });

    // Empfang: ClientConnected
    this.hubConnection.on('ClientConnected', (name: string) => {
      this.messages.push({ name: '', message: `Client ${name} connected`, timestamp: new Date().toLocaleTimeString() });
    });

    // Empfang: ClientDisconnected
    this.hubConnection.on('ClientDisconnected', (name: string) => {
      this.messages.push({ name: '', message: `Client ${name} disconnected`, timestamp: new Date().toLocaleTimeString() });
    });

    // Empfang: AdminNotification
    this.hubConnection.on('AdminNotification', (msg: string) => {
      this.messages.push({ name: 'Admin', message: msg, timestamp: new Date().toLocaleTimeString() });
    });

    // Empfang: NrClientsChanged
    this.hubConnection.on('NrClientsChanged', (nr: number) => {
      this.nrClients = nr;
    });
  }

  async signIn() {
    try {
      // Aufruf: SignIn
      this.isAdmin = await this.hubConnection.invoke<boolean>('SignIn', this.name, this.password);
      this.isSignedIn = true;

      // Topics setzen falls vorhanden
      if (this.topicsOfInterest.length > 0) {
        await this.hubConnection.invoke('RegisterTopicsOfInterest', this.topicsOfInterest);
      }

      if (this.isAdmin) {
        // Als Admin gleich mal die Anzahl holen
        this.nrClients = await this.hubConnection.invoke<number>('GetNrClients');
      }
    } catch (err: any) {
      alert("Error logging in: " + err.message);
    }
  }

  async signOut() {
    await this.hubConnection.invoke('SignOut');
    this.isSignedIn = false;
    this.isAdmin = false;
    this.messages = [];
  }

  async sendMessage() {
    if (!this.currentMessage) return;
    // Aufruf: SendMessage
    await this.hubConnection.invoke('SendMessage', this.name, this.currentMessage, this.currentTopic);
    this.currentMessage = '';
  }

  updateTopics() {
    this.topicsOfInterest = this.topicsOfInterestInput.split(',').map(t => t.trim()).filter(t => t.length > 0);
    if (this.isSignedIn) {
      this.hubConnection.invoke('RegisterTopicsOfInterest', this.topicsOfInterest);
    }
  }
}