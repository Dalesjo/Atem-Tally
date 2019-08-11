import { Component } from '@angular/core';
import { TallyService, Tally } from './tally.service';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  messages: Tally[] = [];
  current: Tally;

  constructor( private tallyService: TallyService) {

    

    tallyService.messages.subscribe(message => {
      console.log("Message recieved", message);
      this.current = message;
      this.messages.push(message);
    });

  }

  send(channel: string) {

    console.log("data", this.current ? "yes" : "no");

    this.tallyService.send({ "program": channel, "preview": this.current ? this.current.program : "" });
  }

}
