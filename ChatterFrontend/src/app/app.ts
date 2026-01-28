import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

interface ChatMessage {
  timestamp: string;
  name: string;
  message: string;
  isSystem?: boolean; // Für Admin Notifications oder Connect/Disconnect
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  // Login State
  name = 'Hansi'; // Standardwert für schnelles Testen
  password = 'abcdefg';
  isSignedIn = false;
  
  // Admin State
  isAdmin = false;
  nrClients = 0;
  
  // Topics Extension
  topicsInput = 'General, Sports';
  topicsOfInterest: string[] = [];

  // Chat
  messages: ChatMessage[] = [];
  currentMessage = '';
  currentTopic = '';

  private connection: HubConnection;

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/hub/chat')
      .build();
  }

  ngOnInit() {
    this.startConnection();
    this.registerSignalREvents();
  }

  private async startConnection() {
    try {
      await this.connection.start();
      console.log('SignalR Connected');
    } catch (err) {
      console.error('SignalR Connection Error:', err);
    }
  }

  private registerSignalREvents() {
    this.connection.on('NewMessage', (name, message, timestamp) => {
      this.messages.push({ name, message, timestamp });
    });

    this.connection.on('ClientConnected', (name) => {
      this.messages.push({ 
        name: '', 
        message: `Client ${name} connected`, 
        timestamp: new Date().toLocaleTimeString(),
        isSystem: true 
      });
    });

    this.connection.on('ClientDisconnected', (name) => {
      this.messages.push({ 
        name: '', 
        message: `Client ${name} disconnected`, 
        timestamp: new Date().toLocaleTimeString(),
        isSystem: true 
      });
    });

    this.connection.on('AdminNotification', (message) => {
      this.messages.push({ 
        name: 'System', 
        message: message, 
        timestamp: new Date().toLocaleTimeString(),
        isSystem: true 
      });
    });

    this.connection.on('NrClientsChanged', (nr) => {
      this.nrClients = nr;
    });
  }

  async signIn() {
    try {
      // Topics parsen
      this.topicsOfInterest = this.topicsInput.split(',').map(t => t.trim()).filter(t => t);
      
      // Rückgabewert ist true, wenn User ein Admin ist
      this.isAdmin = await this.connection.invoke<boolean>('SignIn', this.name, this.password);
      this.isSignedIn = true;

      if (this.topicsOfInterest.length > 0) {
        await this.connection.invoke('RegisterTopicsOfInterest', this.topicsOfInterest);
      }

      if (this.isAdmin) {
        this.nrClients = await this.connection.invoke<number>('GetNrClients');
      }

    } catch (err: any) {
      alert(`Login failed: ${err.message}`);
    }
  }

  async signOut() {
    this.isSignedIn = false;
    this.isAdmin = false;
    this.messages = []; // Chat leeren beim Ausloggen
  }

  async sendMessage() {
    if (!this.currentMessage) return;
    await this.connection.invoke('SendMessage', this.name, this.currentMessage, this.currentTopic);
    this.currentMessage = '';
  }

  async updateTopics() {
    this.topicsOfInterest = this.topicsInput.split(',').map(t => t.trim()).filter(t => t);
    await this.connection.invoke('RegisterTopicsOfInterest', this.topicsOfInterest);
  }
}