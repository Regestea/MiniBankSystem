import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { Card, LoadingState, PageHeader, Stepper, useTransferFlow } from "@minibank/shared/src/index";
import { AccountNumberStep } from "./components/AccountNumberStep";
import { RecipientStep } from "./components/RecipientStep";
import { TransferConfirmation } from "./components/TransferConfirmation";
import { TransferSuccess } from "./components/TransferSuccess";
import "./transfer.css";

const STEPS = ["Details", "Recipient", "Confirm", "Done"];

function indexOf(step: string): number {
  if (step === "details") return 0;
  if (step === "recipient") return 1;
  if (step === "confirm" || step === "processing") return 2;
  return 3;
}

/**
 * Feature: transfer — multi-step flow reusing the shared useTransferFlow
 * state machine (details → recipient → confirm → processing → success).
 * Supports `?to=` preselect from the Recipients feature.
 */
export function TransferScreen(): React.JSX.Element {
  const [params] = useSearchParams();
  const flow = useTransferFlow();
  const preselect = params.get("to");

  useEffect(() => {
    if (preselect) flow.startFromRecipient(preselect);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preselect]);

  return (
    <div className="mobile-screen">
      <PageHeader title="Transfer" subtitle="Send USD by account number." />
      <Stepper steps={STEPS} current={indexOf(flow.step)} />
      <Card>
        {flow.step === "details" && <AccountNumberStep flow={flow} />}
        {flow.step === "recipient" && <RecipientStep flow={flow} />}
        {flow.step === "confirm" && <TransferConfirmation flow={flow} />}
        {flow.step === "processing" && <LoadingState message="Sending your money…" />}
        {flow.step === "success" && <TransferSuccess flow={flow} />}
      </Card>
    </div>
  );
}
