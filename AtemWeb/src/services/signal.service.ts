import { Injectable } from '@angular/core';
import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@aspnet/signalr'
import { Subject } from 'rxjs';

export class SignalService {

  /**
   * Signals if the connection state changed. */
  public connected: Subject<boolean>;

  /** true if connection is established */
  public isConnected: boolean;

  /**
   * sets behaviour when connection is lost or cannot be established. */
  public reconnect: boolean;

  /**
   * Sets milliseconds we need to wait between each reconnect attempt. */
  public reconnectTimeout: number;

  /**
 * Holds the actual connection to the signalR host */
  protected hubConnection: HubConnection;

  /** If we at the moment are trying to connect */
  private connecting: boolean;

  /**
   * timeout handler */
  private reconnector: number;

  constructor(url: string) {
    this.hubConnection = new HubConnectionBuilder().withUrl(url).build();
    this.connected = new Subject<boolean>();
    this.isConnected = false;

    this.connected.subscribe(connected => {
      console.log("isConnected",connected);
      this.isConnected = connected;
    });

    /* Handles connection loss */
    this.hubConnection.onclose(() => {
      this.connected.next(false);

      if (this.reconnect) {
        this.connect();
      }
    });
  }

  connect(): void {

    /* Break if connection is under way */
    if (this.connecting || this.isConnected) {
      return;
    }

    /* Stop reconnection */
    if (this.reconnector) {
      window.clearTimeout(this.reconnector);
      this.reconnector = null;
    }

    /* Try connect to hub */
    this.connecting = true;
    this.hubConnection.start().then(() => {
      this.connected.next(true);
    }).catch((reason) => {
      this.connected.next(false);

      if (this.reconnect) {
        this.reconnector = window.setTimeout(() => {
          this.connect();
        }, 15000);
      }
    }).finally(() => {
      this.connecting = false;
    });
  };

  stop(): void {
    this.hubConnection.stop();
  }
}
