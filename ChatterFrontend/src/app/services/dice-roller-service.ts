import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root',
})
export class DiceRollerService {
  private hubConnection: signalR.HubConnection;
  public diceRoll = signal<number | undefined>(undefined); 

  constructor(){
    this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hub/dice-hub')
    .withAutomaticReconnect()
    .build();

    this.hubConnection.start()
    .then(() => console.log('SignalR connected'))
    .catch(err => console.log(`Error while connecting: ${err}`));

    this.registerServerEvents();
  }

  private registerServerEvents():void{
    this.hubConnection.on('ReceiveDiceRoll', (roll: number) => this.diceRoll.set(roll));

    this.hubConnection.on('ReceiveCreatedDiceRoll', (roll:number) => {
      console.log('Received');
      this.diceRoll.set(roll);
    })
  }

  public async sendRollDice():Promise<void>{
    //const roll = Math.floor(Math.random() * 6) + 1;


   const roll = await this.hubConnection.invoke('CreateDiceRoll');

    console.log(`DiceRollerService::sendRollDeice(${roll})`);

    //this.hubConnection.send('CreateDiceRoll');
    this.hubConnection.send('SendDiceRoll', roll);
  }
}
