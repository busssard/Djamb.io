import React, { FC, useState } from 'react';
import { FormControl, FormGroup } from '@mui/material';
import { quickJoin } from '../../controllers/userController';
import FormTextField from './controls/FormTextField';
import FormSubmitButton from './controls/FormSubmitButton';

type FormState = {
  username: string;
  email: string;
};

const defaultState: FormState = {
  username: '',
  email: '',
};

const QuickJoinForm: FC = () => {
  const [state, setState] = useState(defaultState);

  const submit = () => quickJoin(state.username, state.email || undefined);

  return (
    <div>
      <FormControl component="fieldset" onSubmit={submit}>
        <FormGroup>
          <FormTextField
            label="Username"
            value={state.username}
            onChanged={(e) =>
              setState({
                ...state,
                username: e.target.value,
              })
            }
            onSubmit={submit}
          />
          <FormTextField
            label="Email (optional)"
            value={state.email}
            placeholder="For signing in on other devices"
            onChanged={(e) =>
              setState({
                ...state,
                email: e.target.value,
              })
            }
            onSubmit={submit}
          />
          <br />
          <FormSubmitButton text="Join" onClick={submit} />
        </FormGroup>
      </FormControl>
    </div>
  );
};

export default QuickJoinForm;
