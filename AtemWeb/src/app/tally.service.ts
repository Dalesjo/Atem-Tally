import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SignalService } from '../services/signal.service';


export interface Tally {
  program: string;
  preview: string;
}

@Injectable({
  providedIn: 'root'
})
export class TallyService extends SignalService {

  public messages: Subject<Tally>;

  constructor() {
    
    super("http://192.168.50.52:5000/tally?test=test");

    this.messages = new Subject<Tally>();
    this.reconnect = true;
    this.hubConnection.on("ReceiveTally", (tally: Tally) => {
      this.messages.next(tally);
    });

    this.connect();
  }

  send(data: Tally): void {

    if (this.isConnected) {
      this.hubConnection.send("SendTally", data);
    } else {
      console.log("Message could not be sent");
    }
    
  }


}
