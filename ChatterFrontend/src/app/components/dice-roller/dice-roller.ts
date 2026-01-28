import { Component, computed, inject } from '@angular/core';
import { DiceRollerService } from '../../services/dice-roller-service';

@Component({
  selector: 'app-dice-roller',
  imports: [],
  templateUrl: './dice-roller.html',
  styleUrl: './dice-roller.scss',
})
export class DiceRoller {

  diceRollerService = inject(DiceRollerService);
  rolledNumber =  this.diceRollerService.diceRoll;

  rollDice() {
    this.diceRollerService.sendRollDice();
  }

}
