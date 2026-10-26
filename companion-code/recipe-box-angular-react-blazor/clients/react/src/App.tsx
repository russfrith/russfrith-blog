import { Outlet } from 'react-router';
import { Header } from './header/Header';

export function App() {
  return (
    <>
      <Header />
      <main className="mx-auto max-w-5xl px-4 py-8">
        <Outlet />
      </main>
    </>
  );
}
