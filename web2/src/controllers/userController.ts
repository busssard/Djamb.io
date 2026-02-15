import * as Api from '../utilities/api';
import { loggedIn, restoreSucceeded, restoreFailed } from '../redux/session/actionFactory';
import { store } from '../redux';
import { preloadAllBoards } from './boardController';
import { preloadAllPieceImages } from './imageController';

function postLoginActions(): void {
  preloadAllBoards();
  preloadAllPieceImages();
}

export async function quickJoin(name: string, email?: string): Promise<void> {
  const state = store.getState();
  const apiUrl = state.config.environment.apiUrl;

  const response = await fetch(`${apiUrl}/api/users/quick`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ name, email: email || null }),
  });

  if (!response.ok) {
    const text = await response.text();
    let message = `Registration failed (${response.status})`;
    try {
      const problem = JSON.parse(text);
      if (problem.title) message = problem.title;
    } catch {
      // use default message
    }
    throw new Error(message);
  }

  const session = await response.json();
  const action = loggedIn(session.user);
  store.dispatch(action);

  postLoginActions();
}

export async function restoreSession(): Promise<void> {
  try {
    const user = await Api.users().apiUsersCurrentGet();
    const action = restoreSucceeded(user);
    store.dispatch(action);

    postLoginActions();
  } catch {
    const action = restoreFailed();
    store.dispatch(action);
  }
}
