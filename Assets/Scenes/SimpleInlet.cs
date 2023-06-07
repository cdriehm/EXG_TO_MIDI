using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LSL;

public class SimpleInlet : MonoBehaviour
{
    // UI elements for interacting with the stream
    public InputField StreamNameInputField; // Input field where the user will enter the name of the stream
    public Button ResolveStreamButton; // Button that will trigger the process of resolving the stream
    public Text DataDisplayText; // Text field where the received data will be displayed

    private string StreamName; // Name of the LSL stream to be resolved
    private ContinuousResolver resolver; // Resolver object to continuously resolve the stream
    private StreamInlet inlet; // Inlet object for the resolved stream
    private double max_chunk_duration = 0.2; // Maximum duration of buffer passed to pull_chunk function

    // Buffers for storing data and timestamps from the stream
    private float[,] data_buffer;
    private double[] timestamp_buffer;

    // String to display the received data
    private string dataDisplayString = "";

    void Start()
    {
        // Set up the listener for the Resolve Stream button.
        // When the button is clicked, it will call the ResolveNewStream function.
        if (ResolveStreamButton != null)
            ResolveStreamButton.onClick.AddListener(ResolveNewStream);
    }

    public void ResolveNewStream()
    {
        // Get the name of the stream from the input field
        StreamName = StreamNameInputField.text;

        // If a stream name is provided, set up the resolver with it.
        // If not, use a default name ("EEG") and notify the user.
        if (!StreamName.Equals(""))
            resolver = new ContinuousResolver("name", StreamName);
        else
        {
            Debug.Log("No stream name given. Setting to 'EEG'");
            StreamNameInputField.text = "EEG";
            StreamName = StreamNameInputField.text;
            resolver = new ContinuousResolver("name", StreamName);
        }

        Debug.Log("Opening Stream:" + StreamName);

        // Start the coroutine to resolve the stream and set up the inlet
        StartCoroutine(ResolveExpectedStream());
    }


    IEnumerator ResolveExpectedStream()
    {
        // Continuously resolve the stream until it's found
        var results = resolver.results();
        while (results.Length == 0)
        {
            yield return new WaitForSeconds(.1f);
            results = resolver.results();
        }

        // Once the stream is found, set up the inlet with it
        inlet = new StreamInlet(results[0]);

        // Prepare the buffers to receive the data and timestamps from the stream
        int buf_samples = (int)Mathf.Ceil((float)(inlet.info().nominal_srate() * max_chunk_duration));
        int n_channels = inlet.info().channel_count();
        data_buffer = new float[buf_samples, n_channels];
        timestamp_buffer = new double[buf_samples];
    }

    void Update()
    {
        // If the inlet has been set up, pull data from it
        if (inlet != null)
        {
            int samples_returned = inlet.pull_chunk(data_buffer, timestamp_buffer);

            // If data was received, format it into a string for display
            if (samples_returned > 0)
            {
                for (int i = 0; i < inlet.info().channel_count(); i++)
                {
                    dataDisplayString = string.Format("Channel {0}\n: {1}\n", i, data_buffer[0, i]);
                    Debug.Log(dataDisplayString);
                }
                // Display the data to the screen.
                if (DataDisplayText != null)
                {
                    DataDisplayText.text = dataDisplayString;
                }
            }
        }
    }
}