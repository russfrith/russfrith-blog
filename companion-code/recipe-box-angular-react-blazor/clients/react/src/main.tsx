import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Route, Routes } from 'react-router';
import './styles.css';
import { App } from './App';
import { RecipeList } from './recipe-list/RecipeList';
import { RecipeDetail } from './recipe-detail/RecipeDetail';
import { RecipeForm } from './recipe-form/RecipeForm';

// retry:false keeps e2e timing in line with the other two clients - none
// of them retry failed requests, and a forced-500 test needs the error
// state to show well inside Playwright's default expect timeout.
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<App />}>
            <Route index element={<RecipeList />} />
            <Route path="recipes/new" element={<RecipeForm />} />
            <Route path="recipes/:id/edit" element={<RecipeForm />} />
            <Route path="recipes/:id" element={<RecipeDetail />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
